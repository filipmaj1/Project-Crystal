-- --------------------------------------------------------
-- Host:                         127.0.0.1
-- Server version:               5.6.17 - MySQL Community Server (GPL)
-- Server OS:                    Win64
-- HeidiSQL Version:             10.1.0.5464
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;

-- Dumping structure for table playonline.status
CREATE TABLE IF NOT EXISTS `status` (
  `polId` varchar(50) NOT NULL,
  `isOnline` tinyint(1) unsigned NOT NULL DEFAULT '0',
  `onlineStatus` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `canReceiveMsgs` tinyint(1) unsigned NOT NULL DEFAULT '0',
  `activeHandleId` bigint(20) unsigned DEFAULT NULL,
  `hasActiveCharacter` tinyint(1) unsigned NOT NULL DEFAULT '0',
  `activeCharacterId` bigint(20) unsigned NOT NULL DEFAULT '0',
  `currentContentClass` smallint(5) unsigned NOT NULL DEFAULT '0',
  `lastLoginTime` int(10) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`polId`),
  KEY `FK_status_handles` (`activeHandleId`),
  CONSTRAINT `FK_status_accounts` FOREIGN KEY (`polId`) REFERENCES `accounts` (`polId`) ON DELETE CASCADE,
  CONSTRAINT `FK_status_handles` FOREIGN KEY (`activeHandleId`) REFERENCES `handles` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COMMENT='Stores the current status of this handle such as if online, online state, contentID, comment, etc.';

-- Data exporting was unselected.
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
