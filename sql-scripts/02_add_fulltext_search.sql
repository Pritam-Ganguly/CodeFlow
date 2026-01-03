ALTER TABLE Questions 
ADD COLUMN SearchVector tsvector 
GENERATED ALWAYS AS (to_tsvector('english', Title || ' ' || COALESCE(BodyMarkDown, ''))) STORED;

CREATE INDEX IX_Questions_SearchVector ON Questions USING GIN(SearchVector);