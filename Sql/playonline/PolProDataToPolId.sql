-- --------------------------------------------------------
-- Host:                         127.0.0.1
-- Server version:               5.6.17 - MySQL Community Server (GPL)
-- Server OS:                    Win64
-- HeidiSQL Version:             10.1.0.5464
-- --------------------------------------------------------

-- Dumping structure for function playonline.PolProDataToPolId
DELIMITER //
CREATE FUNCTION `PolProDataToPolId`(
    p_polIdData VARCHAR(8),
    p_volume INT,
    p_domain INT
) RETURNS bigint(20) unsigned
    DETERMINISTIC
BEGIN
    DECLARE v_polId BIGINT UNSIGNED DEFAULT 0;
    DECLARE v_extra BIGINT UNSIGNED;
    DECLARE v_charMap VARCHAR(36);
    DECLARE i INT DEFAULT 1;
    DECLARE v_char CHAR(1);
    DECLARE v_val INT;

    -- If the string isn't exactly 8 characters, return 0 early
    IF CHAR_LENGTH(p_polIdData) <> 8 THEN
        RETURN 0;
    END IF;

    -- Your exact character sequence map
    SET v_charMap = 'EFKAOYMJVNGTDSWBQLPCIRHZXU6328401795';

    -- Base-36 decoding loop (MySQL strings are 1-indexed)
    WHILE i <= 8 DO
        -- Grab the character at position i
        SET v_char = SUBSTRING(p_polIdData, i, 1);
        
        -- Find its 0-based position in the map (LOCATE returns 1-based index, or 0 if not found)
        SET v_val = LOCATE(v_char, v_charMap) - 1;
        
        -- If an invalid character is passed, you might want to return 0
        IF v_val < 0 THEN
            RETURN 0;
        END IF;

        -- Shift and add the base-36 value
        SET v_polId = (v_polId * 36) + v_val;
        
        SET i = i + 1;
    END WHILE;

    -- Reconstruct the 'extra' bits: (((volume & 0xFFFF) << 7) | (domain & 0x7F))
    -- Note: MySQL automatically takes care of the byte masking during bitwise operations
    SET v_extra = ((p_volume & 0xFFFF) << 7) | (p_domain & 0x7F);

    -- Apply the final shift and merge: polId | (extra << 41)
    RETURN v_polId | (v_extra << 41);
END//
DELIMITER ;
