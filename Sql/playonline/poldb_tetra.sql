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

-- Dumping structure for table playonline.poldb_tetra
CREATE TABLE IF NOT EXISTS `poldb_tetra` (
  `contentId` bigint(20) unsigned NOT NULL,
  `contentSubId` mediumint(8) unsigned NOT NULL DEFAULT '0',
  `polId` bigint(20) unsigned NOT NULL DEFAULT '0',
  `name` varchar(16) NOT NULL DEFAULT '',
  `purpose` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `lang` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `status` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `contentsNumber` smallint(5) unsigned NOT NULL DEFAULT '0',
  `averageRank` varchar(64) NOT NULL DEFAULT '0',
  `cardLevel` int(11) NOT NULL DEFAULT '0',
  `currentTitle` tinyint(3) unsigned NOT NULL DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- Data exporting was unselected.
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
