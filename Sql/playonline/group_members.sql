-- --------------------------------------------------------
-- Host:                         147.182.172.129
-- Server version:               11.8.6-MariaDB-0+deb13u1 from Debian - -- Please help get to 10k stars at https://github.com/MariaDB/Server
-- Server OS:                    debian-linux-gnu
-- HeidiSQL Version:             10.1.0.5464
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;


-- Dumping database structure for playonline
CREATE DATABASE IF NOT EXISTS `playonline` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci */;
USE `playonline`;

-- Dumping structure for table playonline.group_members
CREATE TABLE IF NOT EXISTS `group_members` (
  `groupId` bigint(20) unsigned NOT NULL,
  `polId` varchar(50) NOT NULL,
  `handleId` bigint(20) unsigned NOT NULL,
  `rank` tinyint(3) unsigned NOT NULL DEFAULT 3,
  `onlineStatus` tinyint(3) unsigned NOT NULL DEFAULT 3,
  `comment` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `joinDate` timestamp NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`groupId`,`polId`,`handleId`),
  KEY `FK_polId` (`polId`),
  KEY `FK_handleId` (`handleId`),
  CONSTRAINT `FK_groupId` FOREIGN KEY (`groupId`) REFERENCES `groups` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_handleId` FOREIGN KEY (`handleId`) REFERENCES `handles` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_polId` FOREIGN KEY (`polId`) REFERENCES `accounts` (`polId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci;

-- Dumping data for table playonline.group_members: ~2 rows (approximately)
DELETE FROM `group_members`;
/*!40000 ALTER TABLE `group_members` DISABLE KEYS */;
INSERT INTO `group_members` (`groupId`, `polId`, `handleId`, `rank`, `onlineStatus`, `comment`, `joinDate`) VALUES
	(1, 'VBEV2787', 4, 5, 3, '', '2026-07-13 16:09:00'),
	(1, 'XQNR8062', 8, 3, 3, '', '2026-07-19 16:40:30');
/*!40000 ALTER TABLE `group_members` ENABLE KEYS */;

/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
