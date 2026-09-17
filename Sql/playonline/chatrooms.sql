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

-- Dumping structure for table playonline.chatrooms
CREATE TABLE IF NOT EXISTS `chatrooms` (
  `channel` varchar(50) NOT NULL,
  `chatroomName` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `usersCurrent` smallint(5) unsigned NOT NULL DEFAULT 0,
  `usersMax` smallint(5) unsigned NOT NULL DEFAULT 0,
  `permanent` tinyint(1) unsigned NOT NULL DEFAULT 0,
  `hasPassword` tinyint(1) unsigned NOT NULL DEFAULT 0,
  `memberCode` smallint(5) unsigned NOT NULL DEFAULT 0,
  `purposeCode` smallint(5) unsigned NOT NULL DEFAULT 0,
  `languageCode` smallint(5) unsigned NOT NULL DEFAULT 0,
  `zoneCode` smallint(5) unsigned NOT NULL DEFAULT 0,
  `creationTime` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`channel`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci COMMENT='Table of chatrooms currently active. This is cleared on POL-AUTH restart.';

-- Data exporting was unselected.
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
