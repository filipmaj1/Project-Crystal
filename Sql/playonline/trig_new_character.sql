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

-- Dumping structure for trigger playonline.trig_new_character
-- Creates a blank PolBES profile row in the table that serves NEW.contentClass.
-- Column lists mirror POLProfile/PolDb/Tables/*.cs; every mapped column is written
-- explicitly because PolDbTable.ReadSql calls GetString/GetInt* directly and will
-- throw on a NULL.
SET @OLDTMP_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_ENGINE_SUBSTITUTION';
DELIMITER //
CREATE TRIGGER `trig_new_character` AFTER INSERT ON `characters` FOR EACH ROW BEGIN
	CASE NEW.contentClass

		-- 1 = Final Fantasy XI
		WHEN 1 THEN
			INSERT INTO `poldb_ffxi`
				(`contentId`, `contentSubId`, `name`, `purpose`, `lang`, `worldName`,
				 `countryId`, `zoneId`, `jobId`, `jobLevel`, `raceId`)
			VALUES
				(NEW.id, NEW.subId, '', 0, 0, '', 0, 0, 0, 0, 0);

		-- 2 = Tetra Master
		WHEN 2 THEN
			INSERT INTO `poldb_tetramaster`
				(`contentId`, `contentSubId`, `name`, `purpose`, `lang`, `status`,
				 `contentsNo`, `polId`, `averageRank`, `jobLevel`, `cardLevel`, `currentTitle`)
			VALUES
				(NEW.id, NEW.subId, '', 0, 0, 0, 0,
				 PolProDataToPolId(NEW.polId, 0, 0), '', 0, 0, 0);

		-- 3 = Janghourou
		WHEN 3 THEN
			INSERT INTO `poldb_jang`
				(`contentId`, `contentSubId`, `name`, `purpose`, `lang`, `status`,
				 `contentsNo`, `polId`)
			VALUES
				(NEW.id, NEW.subId, '', 0, 0, 0, 0,
				 PolProDataToPolId(NEW.polId, 0, 0));

		-- 4 = Front Mission Online
		WHEN 4 THEN
			INSERT INTO `poldb_fmo`
				(`contentId`, `contentSubId`, `lang`, `firstName`, `lastName`,
				 `worldName`, `countryId`, `zoneId`, `name`)
			VALUES
				(NEW.id, NEW.subId, 0, '', '', '', 0, 0, '');

		-- 10 = Dirge of Cerberus
		WHEN 10 THEN
			INSERT INTO `poldb_dc`
				(`contentId`, `contentSubId`, `lang`, `name`, `glevel`, `class`,
				 `mvp`, `rankPoint`)
			VALUES
				(NEW.id, NEW.subId, 0, '', 0, 0, 0, 0);

		-- 11 = Fantasy Earth
		WHEN 11 THEN
			INSERT INTO `poldb_fe`
				(`contentId`, `contentSubId`, `name`, `sex`, `world`, `nation`,
				 `class`, `level`, `lang`)
			VALUES
				(NEW.id, NEW.subId, '', '', '', '', '', '', 0);

		-- Content classes 1000+ (PlayOnline profile, chat, zone count, KB) are keyed
		-- by handleId, not contentId, and are seeded by trig_new_handle instead.
		ELSE BEGIN END;

	END CASE;
END//
DELIMITER ;
SET SQL_MODE=@OLDTMP_SQL_MODE;

/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
