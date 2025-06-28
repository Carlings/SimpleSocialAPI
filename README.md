
# SimpleSocialAPI

This is a minimal RESTful API for a simple social media application, built with ASP.NET Core, Dapper, and PostgreSQL.

---

## Features

- Create and view posts  
- Follow other users  
- Like and unlike posts  
- Each post includes:  
  - Title  
  - Body  
  - Author  
  - Created timestamp  
- Data persistence via Dapper with PostgreSQL  
- Basic unit testing using NUnit and in-memory SQLite  

---

## Requirements

- .NET 8 SDK or newer  
- PostgreSQL server (version 12+ recommended)  
- Any modern IDE (Visual Studio 2022, VS Code, Rider, etc.)  

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/yourusername/SimpleSocialAPI.git
cd SimpleSocialAPI
```

### 2. Configure your PostgreSQL connection string

Edit `appsettings.json` or your configuration source:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=SimpleSocialDb;Username=postgres;Password=your_password"
  }
}
```

### 3. Ensure PostgreSQL is running locally

Make sure your PostgreSQL server is installed and running on port 5432 (default).

### 4. Create database schema manually

Dapper does not manage migrations, so create tables manually or use a SQL script:

```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100) UNIQUE NOT NULL,
    displayname VARCHAR(100) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL
);

CREATE TABLE posts (
    id SERIAL PRIMARY KEY,
    title TEXT NOT NULL,
    body TEXT NOT NULL,
    authorid INT NOT NULL REFERENCES users(id),
    createdat TIMESTAMPTZ NOT NULL
);

CREATE TABLE follows (
    follower_id INT NOT NULL REFERENCES users(id),
    followed_id INT NOT NULL REFERENCES users(id),
    PRIMARY KEY (follower_id, followed_id)
);

CREATE TABLE likes (
    user_id INT NOT NULL REFERENCES users(id),
    post_id INT NOT NULL REFERENCES posts(id),
    PRIMARY KEY (user_id, post_id)
);
```

### 5. Run the API

```bash
dotnet run
```

The API will be available at:

```
https://localhost:5001/
```

---

## Running Unit Tests

Navigate to the test project folder (e.g., `SimpleSocialAPI.Tests`) and run:

```bash
dotnet test
```

Tests use SQLite in-memory database for fast, isolated execution without external dependencies.

---

## Task Feedback

Please fill this section after completing your tasks:

- **Q:** Was it easy to complete the task using AI?  
  **A:** Yes, the guidance and code samples were clear and helpful.

- **Q:** How long did the task take you?  
  **A:** About 2 hours, mostly spent setting up PostgreSQL and testing with Dapper.

- **Q:** Was the code ready to run after generation? What did you have to adjust?  
  **A:** Mostly yes, needed to add manual DB schema creation and tweak connection strings.

- **Q:** What challenges did you face?  
  **A:** Understanding how to handle migrations without EF Core and testing Minimal APIs.

- **Q:** Which prompt or approach helped you most?  
  **A:** Asking for Minimal API testing strategies without MVC or WebApplicationFactory.

---
