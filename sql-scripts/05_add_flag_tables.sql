CREATE TABLE FlagTypes (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(50) UNIQUE NOT NULL,
    Description TEXT NOT NULL,
    SeverityLevel INTEGER NOT NULL DEFAULT 1, -- 1=Low, 2=Medium, 3=High
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW()
);

INSERT INTO FlagTypes (Name, Description, SeverityLevel) VALUES
    ('Spam', 'Unsolicited commercial content', 3),
    ('Offensive', 'Hate speech, harassment', 3),
    ('Inappropriate', 'Not suitable for all audiences', 2),
    ('Duplicate', 'Exact duplicate of existing content', 1),
    ('Inaccurate', 'Factually incorrect information', 2),
    ('Plagiarism', 'Copied content without attribution', 3),
    ('Other', 'Other issues not covered above', 1);

CREATE TABLE Flags (
    Id SERIAL PRIMARY KEY,
    FlagTypeId INTEGER NOT NULL REFERENCES FlagTypes(Id) ON DELETE CASCADE,
    PostType VARCHAR(20) NOT NULL CHECK (PostType IN ('Question', 'Answer', 'Comment')),
    PostId INTEGER NOT NULL,
    ReportingUserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    Reason TEXT,
    Status VARCHAR(20) NOT NULL DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Reviewed', 'Resolved', 'Dismissed')),
    ResolutionNotes TEXT,
    ResolvedByUserId INTEGER REFERENCES Users(Id) ON DELETE SET NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
    UpdatedAt TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IX_Flags_Status ON Flags(Status) WHERE Status = 'Pending';
CREATE INDEX IX_Flags_Post ON Flags(PostType, PostId);
