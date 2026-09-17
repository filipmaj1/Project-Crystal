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

-- Dumping structure for table playonline.friendlist
CREATE TABLE IF NOT EXISTS `friendlist` (
  `ownerPolId` varchar(50) NOT NULL,
  `friendPolId` varchar(50) NOT NULL,
  `friendHandleId` bigint(20) unsigned NOT NULL,
  `blacklist` tinyint(1) unsigned NOT NULL,
  `temp` tinyint(1) unsigned NOT NULL,
  `level` tinyint(3) unsigned NOT NULL,
  `grp` tinyint(3) unsigned NOT NULL,
  `creationPosition` tinyint(3) unsigned NOT NULL,
  `customPosition` tinyint(3) unsigned NOT NULL,
  `name` varchar(50) NOT NULL,
  PRIMARY KEY (`ownerPolId`,`friendPolId`,`friendHandleId`),
  KEY `FK_Accounts_FriendPolId` (`friendPolId`),
  KEY `FK_Handles_FriendHandleId` (`friendHandleId`),
  CONSTRAINT `FK_Accounts_FriendPolId` FOREIGN KEY (`friendPolId`) REFERENCES `accounts` (`polId`) ON DELETE NO ACTION ON UPDATE CASCADE,
  CONSTRAINT `FK_Accounts_OwnerPolId` FOREIGN KEY (`ownerPolId`) REFERENCES `accounts` (`polId`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_Handles_FriendHandleId` FOREIGN KEY (`friendHandleId`) REFERENCES `handles` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- Data exporting was unselected.
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
