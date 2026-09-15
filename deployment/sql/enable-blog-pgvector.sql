-- Optional: install the matching pgvector extension on the PostgreSQL server first.
-- Run after the CompleteWebsiteCms EF migration, in the intended database only.
-- This script does not call an embedding provider or enable the application feature.
CREATE EXTENSION IF NOT EXISTS vector;
ALTER TABLE "BlogEmbeddings" ADD COLUMN IF NOT EXISTS "Embedding" vector(384);
CREATE INDEX IF NOT EXISTS "IX_BlogEmbeddings_Vector" ON "BlogEmbeddings" USING hnsw ("Embedding" vector_cosine_ops);
