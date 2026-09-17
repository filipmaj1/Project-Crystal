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

-- Dumping structure for table playonline.chatrooms_permanent
CREATE TABLE IF NOT EXISTS `chatrooms_permanent` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `chatroomName` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `zoneCode` smallint(5) unsigned NOT NULL DEFAULT 0,
  `usersMax` smallint(5) unsigned NOT NULL DEFAULT 0,
  `memberCode` smallint(5) unsigned NOT NULL DEFAULT 0,
  `purposeCode` smallint(5) unsigned NOT NULL DEFAULT 0,
  `languageCode` smallint(5) unsigned NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Chatrooms to create on boot that exist even with 0 users.';

-- Dumping data for table playonline.chatrooms_permanent: ~0 rows (approximately)
DELETE FROM `chatrooms_permanent`;
/*!40000 ALTER TABLE `chatrooms_permanent` DISABLE KEYS */;
INSERT INTO `chatrooms_permanent` (`id`, `chatroomName`, `zoneCode`, `usersMax`, `memberCode`, `purposeCode`, `languageCode`) VALUES
	(1, '出会いの広場', 1100, 20, 102, 202, 300),
	(2, '初心者の館', 1100, 20, 100, 202, 300),
	(3, '旅立ちの部屋', 1100, 20, 100, 200, 300),
	(4, 'Novice_Hall', 1100, 20, 100, 202, 301),
	(5, 'Town_Square', 1100, 20, 102, 201, 301),
	(6, 'Traveller\'s_Haven', 1100, 20, 100, 200, 301);
/*!40000 ALTER TABLE `chatrooms_permanent` ENABLE KEYS */;

/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
