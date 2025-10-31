-- PostgreSQL 資料庫初始化腳本
-- 用於建立資料庫和使用者

-- 建立資料庫
CREATE DATABASE blogsystem;

-- 建立使用者 (如果不存在)
DO
$$
BEGIN
   IF NOT EXISTS (SELECT FROM pg_catalog.pg_user WHERE usename = 'bloguser') THEN
      CREATE USER bloguser WITH PASSWORD 'blogpass123';
   END IF;
END
$$;

-- 授予權限
GRANT ALL PRIVILEGES ON DATABASE blogsystem TO bloguser;

-- 連接到資料庫
\c blogsystem

-- 授予 schema 權限
GRANT ALL ON SCHEMA public TO bloguser;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO bloguser;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO bloguser;

-- 設定預設權限
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO bloguser;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO bloguser;

-- 啟用 UUID 擴充功能
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 建立全文檢索配置 (中文支援)
-- 注意：這需要安裝 pg_trgm 擴充功能
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- 顯示成功訊息
DO $$
BEGIN
    RAISE NOTICE 'Database initialization completed successfully!';
    RAISE NOTICE 'Database: blogsystem';
    RAISE NOTICE 'User: bloguser';
END $$;
