SELECT
	Friend.ownerPolId AS polId,
	Friend.creationPosition AS friendFLNum,
	(SELECT creationPosition FROM friendlist 
		WHERE 
			friendlist.ownerPolId = Friend.friendPolId AND
			friendlist.friendPolId = Friend.ownerPolId AND
			friendlist.friendHandleId = status.activeHandleId
		) AS myFLNum,
   handles.creationPosition AS activeHandleNumber,
   status.hasActiveCharacter,
   characters.creationPosition AS activeCharacterNumber,
   status.isOnline,
   status.canReceiveMsgs,
   status.onlineStatus,
   status.currentContentId,
   portraitId,
   COMMENT
FROM friendlist AS Friend
   LEFT JOIN status   	ON status.polId  	 = Friend.ownerPolId
   LEFT JOIN handles    ON handles.id      = status.activeHandleId
   LEFT JOIN characters ON characters.id   = status.activeCharacterId
   LEFT JOIN profiles_handle ON handleId   = status.activeHandleId
WHERE 
	Friend.friendPolId = 'PQNQ5974' AND 
	Friend.blacklist = 0 AND 
	Friend.temp = 0 AND
	Friend.friendHandleId = (SELECT activeHandleId FROM status AS MyStatus WHERE polId = Friend.friendPolId) AND
	EXISTS (
		SELECT * FROM friendlist AS Myself
		WHERE 
			Myself.ownerPolId = Friend.friendPolId AND 
			Myself.friendPolId = Friend.ownerPolId AND 
			Myself.blacklist = 0 AND 
			Myself.temp = 0 AND
			Myself.friendHandleId = status.activeHandleId
	);