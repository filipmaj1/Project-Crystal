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

-- Dumping structure for table playonline.poldb_profiles
CREATE TABLE IF NOT EXISTS `poldb_profiles` (
  `handleId` bigint(20) unsigned NOT NULL,
  `portraitId` int(10) unsigned NOT NULL DEFAULT '0',
  `lastUpdate` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `age` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `ageVisibility` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `sex` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `locationContinent` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `locationCountry` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `locationProvinceState` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `locale` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `language1` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `language2` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `language3` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `job` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `interest1` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `interest2` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `interest3` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `purpose` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `email` varchar(322) NOT NULL DEFAULT 'No Mail address',
  PRIMARY KEY (`handleId`),
  CONSTRAINT `FK_profiles_handles` FOREIGN KEY (`handleId`) REFERENCES `handles` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- Data exporting was unselected.
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
