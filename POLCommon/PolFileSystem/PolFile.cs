/*
===========================================================================
Copyright (C) 2019-2026 Project Crystal Dev Team

This file is part of Project Crystal Server.

Project Crystal Server is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Project Crystal Server is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with Project Crystal Server. If not, see <https://www.gnu.org/licenses/>.
===========================================================================
*/

using System;
using System.Diagnostics;
using System.IO;

namespace Crystal.Common.PolFileSystem
{
    public class PolFile
    {
        private object LockObj;
        public readonly string FilePath;
        public uint NumEntries;
        public readonly int MaxEntries;
        public readonly int EntrySize;
        public readonly int DataOffset;
        public readonly int CountOffset;

        protected internal PolFile(object fileLock, string path, int maxEntries, int entrySize, int dataOffset = 0, int countOffset = -1)
        {
            FilePath = path;
            MaxEntries = maxEntries;
            EntrySize = entrySize;
            DataOffset = dataOffset;
            CountOffset = countOffset;
            LockObj = fileLock ?? new();
        }

        protected internal PolFile(string path)
        {
            FilePath = path;
            MaxEntries = -1;
            EntrySize = -1;
            DataOffset = -1;
            CountOffset = -1;
        }
        
        public bool InitFile(int size)
        {
            try
            {
                lock (LockObj)
                {
                    // Delete first
                    if (File.Exists(FilePath))
                        File.Delete(FilePath);

                    // Write the data
                    FileInfo file = new FileInfo(FilePath);
                    file.Directory.Create();
                    byte[] initialFile = new byte[size];
                    File.WriteAllBytes(FilePath, initialFile);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool DeleteFile()
        {
            try
            {
                lock (LockObj)
                {
                    if (File.Exists(FilePath))
                        File.Delete(FilePath);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool InsertNewEntry(ReadOnlySpan<byte> data)
        {
            FileStream file = null;
            try
            {
                lock (LockObj)
                {
                    using (file = File.Open(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                    { 
                        file.Seek(DataOffset, SeekOrigin.Begin);
                        for (int i = 0; i < MaxEntries; i++)
                        {
                            int peek = file.ReadByte();
                            file.Seek(-1, SeekOrigin.Current);
                            if (peek == 0)
                            {
                                file.Write(data.ToArray(), 0, EntrySize);
                                if (CountOffset != -1)
                                {
                                    file.Seek(CountOffset, SeekOrigin.Begin);
                                    file.Write(BitConverter.GetBytes(++NumEntries));
                                }
                                break;
                            }
                            file.Seek(EntrySize, SeekOrigin.Current);
                        }
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine($"[PolFileSys] Could not insert entry: {e.Message}");
                return false;
            }
            finally
            {
                file?.Close();
            }
            return true;
        }

        public bool InsertNewEntryAtPosition(uint position, ReadOnlySpan<byte> data)
        {
            FileStream file = null;
            try
            {
                lock (LockObj)
                {
                    using (file = File.Open(FilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                    { 
                        file.Seek((EntrySize * (int)position) + DataOffset, SeekOrigin.Begin);
                        file.Write(data.ToArray(), 0, EntrySize);
                        if (CountOffset != -1)
                        {
                            file.Seek(CountOffset, SeekOrigin.Begin);
                            file.Write(BitConverter.GetBytes(++NumEntries));
                        }
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine($"[PolFileSys] Could not insert entry @ {position}: {e.Message}");
                return false;
            }
            finally
            {
                file?.Close();
            }
            return true;
        }

        public bool UpdateEntryWithId(ulong polId, ReadOnlySpan<byte> data)
        {
            byte[] idBytes = new byte[8];
            FileStream file = null;
            try
            {
                lock (LockObj)
                {
                    using (file = File.Open(FilePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
                    {
                        file.Seek(DataOffset, SeekOrigin.Begin);
                        for (int i = 0; i < NumEntries; i++)
                        {
                            file.ReadExactly(idBytes);
                            if (BitConverter.ToUInt64(idBytes) == polId)
                            {
                                file.Seek(-8, SeekOrigin.Current);
                                file.Write(data.ToArray(), 0, EntrySize);
                                break;
                            }
                            file.Seek(EntrySize, SeekOrigin.Current);
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                file?.Close();
            }
            return true;
        }

        public bool UpdateEntryAtPosition(uint position, ReadOnlySpan<byte> data)
        {
            FileStream file = null;
            try
            {
                lock (LockObj)
                {
                    using (file = File.Open(FilePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
                    {
                        file.Seek((EntrySize * (int)position) + DataOffset, SeekOrigin.Begin);
                        file.Write(data.ToArray(), 0, EntrySize);
                    }
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                file?.Close();
            }
            return true;
        }

        public bool DeleteEntryWithId(ulong polId)
        {
            byte[] idBytes = BitConverter.GetBytes((long)-1);
            FileStream file = null;
            try
            {
                lock (LockObj)
                {
                    using (file = File.Open(FilePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
                    {
                        file.Seek(DataOffset, SeekOrigin.Begin);
                        for (int i = 0; i < NumEntries; i++)
                        {
                            file.ReadExactly(idBytes);
                            if (BitConverter.ToUInt64(idBytes) == polId)
                            {
                                file.Seek(-8, SeekOrigin.Current);
                                file.Write(idBytes);
                                if (CountOffset != -1)
                                {
                                    file.Seek(CountOffset, SeekOrigin.Begin);
                                    file.Write(BitConverter.GetBytes(--NumEntries));
                                }
                                break;
                            }
                            file.Seek(EntrySize, SeekOrigin.Current);
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                file?.Close();
            }
            return true;
        }

        public bool DeleteEntryAtPosition(uint position)
        {
            byte[] zeroData = new byte[EntrySize];
            return UpdateEntryAtPosition(position, zeroData);
        }

        public FileStream GetDirectFileStream()
        {
            if ((MaxEntries & EntrySize & DataOffset & CountOffset & -1) == -1)
            {
                return File.Open(FilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            }
            Debug.Assert(true, "Tried to get a filestream from a non-direct file");
            return null;
        }
    }
}
