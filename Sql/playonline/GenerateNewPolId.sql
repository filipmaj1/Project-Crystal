-- --------------------------------------------------------
-- Host:                         127.0.0.1
-- Server version:               5.6.17 - MySQL Community Server (GPL)
-- Server OS:                    Win64
-- HeidiSQL Version:             10.1.0.5464
-- --------------------------------------------------------

-- Dumping structure for function playonline.GenerateNewPolId
DELIMITER //
CREATE FUNCTION `GenerateNewPolId`() RETURNS varchar(8) CHARSET latin1
BEGIN
    DECLARE candidateCode VARCHAR(8);
    DECLARE letters1 VARCHAR(25);
    DECLARE letters VARCHAR(26);
    DECLARE numbers VARCHAR(10);
    DECLARE isTaken INT DEFAULT 1;
    DECLARE loopCount INT DEFAULT 0;

    SET letters1 = 'ABCDEFGHIJKLMNOPQRSTVWXYZ';
    SET letters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    SET numbers = '0123456789';

    -- Keep looping until we find a code that doesn't exist in the table
    WHILE isTaken > 0 DO
        -- Randomly build [A-Z][A-Z][A-Z][A-Z][0-9][0-9][0-9][0-9]
        -- Note: Changed 'candidate' to 'candidateCode' to match declaration
        SET candidateCode = CONCAT(
            SUBSTRING(letters1, FLOOR(1 + RAND() * 26), 1),
            SUBSTRING(letters, FLOOR(1 + RAND() * 26), 1),
            SUBSTRING(letters, FLOOR(1 + RAND() * 26), 1),
            SUBSTRING(letters, FLOOR(1 + RAND() * 26), 1),
            SUBSTRING(numbers, FLOOR(1 + RAND() * 10), 1),
            SUBSTRING(numbers, FLOOR(1 + RAND() * 10), 1),
            SUBSTRING(numbers, FLOOR(1 + RAND() * 10), 1),
            SUBSTRING(numbers, FLOOR(1 + RAND() * 10), 1)
        );

        -- Check if this code is already taken in the accounts table
        SELECT COUNT(*) INTO isTaken 
        FROM accounts 
        WHERE polId = candidateCode;

        -- Safety mechanism to prevent an infinite loop
        SET loopCount = loopCount + 1;
        IF loopCount > 1000 THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Code generation timed out after 1000 attempts.';
        END IF;
    END WHILE;

    RETURN candidateCode;
END//
DELIMITER ;
