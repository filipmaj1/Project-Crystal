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

-- Dumping structure for table playonline.characters
CREATE TABLE IF NOT EXISTS `characters` (
  `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `subId` mediumint(8) unsigned NOT NULL DEFAULT '0',
  `polId` varchar(10) NOT NULL,
  `creationPosition` tinyint(3) unsigned NOT NULL,
  `customPosition` tinyint(3) unsigned NOT NULL,
  `handleCreationPosition` tinyint(3) unsigned NOT NULL DEFAULT '255',
  `contentClass` smallint(5) unsigned NOT NULL,
  `linkPosition` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `name` varchar(16) NOT NULL DEFAULT '',
  `info` varchar(64) NOT NULL DEFAULT '',
  `isPaid` tinyint(1) unsigned NOT NULL DEFAULT '1',
  PRIMARY KEY (`id`),
  KEY `FK_contentids_accounts` (`polId`),
  CONSTRAINT `FK_contentids_accounts` FOREIGN KEY (`polId`) REFERENCES `accounts` (`polId`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- Data exporting was unselected.
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
