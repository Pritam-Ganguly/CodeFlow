CREATE TABLE UserProfiles(
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    Bio TEXT,
    Location VARCHAR(100),
    Website VARCHAR(500),
    ProfilePicture BYTEA,
    ProfilePictureMimeType VARCHAR(50),
    ProfilePictureFileName VARCHAR(255),
    LastActive TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdateAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE ReputationTransactions(
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    Amount INTEGER NOT NULL CHECK (Amount != 0),
    TransactionType VARCHAR(50) NOT NULL,
    RelatedPostId INTEGER NULL,
    RelatedPostType VARCHAR(20) NULL CHECK (RelatedPostType IN ('Question', 'Answer', 'Comment')),
    ActingUserId INTEGER NULL REFERENCES Users(Id) ON DELETE SET NULL,
    Description TEXT NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
);

CREATE TABLE Badges(
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(50) NOT NULL UNIQUE,
    Description VARCHAR(200) NOT NULL,
    IconURL VARCHAR(500) NULL,
    BadgeType TEXT NOT NULL CHECK (BadgeType IN ('bronze', 'silver', 'gold')),
    TriggerCondition TEXT NOT NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
);

CREATE TABLE UserBadges(
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    BadgeId INTEGER NOT NULL REFERENCES Badges(Id) ON DELETE CASCADE,
    AwardedAT TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    UNIQUE(UserId, BadgeId)
);

CREATE TABLE UserActivities(
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    ActivityType VARCHAR(50) NOT NULL,
    TargetEntityType VARCHAR(20) NOT NULL,
    TargetEntityId INTEGER NULL,
    MetaData JSONB NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    CONSTRAINT CHK_ActivityType CHECK (ActivityType IN (
        'question_asked',
        'answer_posted', 
        'comment_added',
        'vote_cast',
        'post_edited',
        'answer_accepted',
        'answer_accepted_owner',
        'badge_earned',
        'reputation_changed'
    )),
    CONSTRAINT CHK_TargetEntityType CHECK (TargetEntityType IN (
        'question',
        'answer', 
        'comment',
        'vote',
        'user',
        'badge',
        'system'
    )),
    CONSTRAINT CHK_TargetEntityId CHECK (
        (TargetEntityType IN ('question', 'answer', 'comment', 'vote', 'user', 'badge') AND TargetEntityId IS NOT NULL) 
        OR (TargetEntityType = 'system' AND TargetEntityId IS NULL)
    )
);

CREATE INDEX IX_UserProfiles_UserId ON UserProfiles(UserId);
CREATE INDEX IX_UserProfiles_LastActive ON UserProfiles(LastActive DESC);
CREATE INDEX IX_UserProfiles_Location ON UserProfiles(Location) WHERE Location IS NOT NULL;
CREATE INDEX IX_ReputationTransactions_User_Created ON ReputationTransactions(UserId, CreatedAt DESC);
CREATE INDEX IX_ReputationTransactions_Type ON ReputationTransactions(TransactionType);
CREATE INDEX IX_ReputationTransactions_ActingUser ON ReputationTransactions(ActingUserId) WHERE ActingUserId IS NOT NULL;
CREATE INDEX IX_ReputationTransactions_UserId ON ReputationTransactions(UserId);
CREATE INDEX IX_ReputationTransactions_CreatedAt ON ReputationTransactions(CreatedAt);
CREATE INDEX IX_ReputationTransactions_RelatedPost ON ReputationTransactions(RelatedPostId, RelatedPostType);
CREATE INDEX IX_Badges_Type ON Badges(BadgeType);
CREATE INDEX IX_Badges_Name ON Badges(Name);
CREATE INDEX IX_UserBadges_UserId ON UserBadges(UserId);
CREATE INDEX IX_UserBadges_BadgeId ON UserBadges(BadgeId);
CREATE INDEX IX_UserBadges_AwardedAt ON UserBadges(AwardedAt DESC);
CREATE INDEX IX_UserActivites_UserId_CreatedAt ON UserActivities(UserId, CreatedAt DESC);
CREATE INDEX IX_UserActivites_ActivityType ON UserActivities(ActivityType);
CREATE INDEX IX_UserActivites_TargetEntity ON UserActivities(TargetEntityType, TargetEntityId);
CREATE INDEX IX_UserActivites_CreatedAt ON UserActivities(CreatedAt DESC);
CREATE INDEX IX_UserActivites_MetaData ON UserActivities USING GIN (MetaData);
CREATE INDEX IX_UserActivities_CreatedAt_Type ON UserActivities(CreatedAt DESC, ActivityType);
CREATE INDEX IX_UserActivities_User_Type ON UserActivities(UserId, ActivityType);
CREATE INDEX IX_UserActivities_User_Created_Type ON UserActivities(UserId, CreatedAt DESC, ActivityType);