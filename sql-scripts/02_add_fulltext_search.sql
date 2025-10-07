ALTER TABLE Questions 
ADD COLUMN SearchVector tsvector 
GENERATED ALWAYS AS (to_tsvector('english', Title || ' ' || Body)) STORED;

CREATE INDEX IX_Questions_SearchVector ON Questions USING GIN(SearchVector);