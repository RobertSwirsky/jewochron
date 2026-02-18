# Yahrzeit Database Setup

## MySQL Installation

1. **Install MySQL Server** (if not already installed):
   - Download from: https://dev.mysql.com/downloads/mysql/
   - Or use MySQL in Docker: `docker run --name mysql-jewochron -e MYSQL_ROOT_PASSWORD=your_password -p 3306:3306 -d mysql:latest`

2. **Create the Database**:
   ```sql
   CREATE DATABASE jewochron CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
   ```

3. **Create a User** (optional, for better security):
   ```sql
   CREATE USER 'jewochron_user'@'localhost' IDENTIFIED BY 'secure_password';
   GRANT ALL PRIVILEGES ON jewochron.* TO 'jewochron_user'@'localhost';
   FLUSH PRIVILEGES;
   ```

## Connection String Configuration

Update the connection string in `App.xaml.cs`:

```csharp
string connectionString = "server=localhost;port=3306;database=jewochron;user=root;password=your_password";
```

### Connection String Parameters:
- **server**: MySQL server address (usually `localhost`)
- **port**: MySQL port (default is `3306`)
- **database**: Database name (`jewochron`)
- **user**: MySQL username
- **password**: MySQL password

### Example Connection Strings:

**Local Development:**
```
server=localhost;port=3306;database=jewochron;user=root;password=mypassword
```

**Docker MySQL:**
```
server=localhost;port=3306;database=jewochron;user=root;password=docker_password
```

**Remote Server:**
```
server=192.168.1.100;port=3306;database=jewochron;user=jewochron_user;password=secure_pass
```

## Database Schema

The application will automatically create the `yahrzeits` table with the following structure:

```sql
CREATE TABLE yahrzeits (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    HebrewMonth INT NOT NULL,
    HebrewDay INT NOT NULL,
    HebrewYear INT NOT NULL,
    NameEnglish VARCHAR(200) NOT NULL,
    NameHebrew VARCHAR(200) NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_hebrew_date (HebrewMonth, HebrewDay)
);
```

## Accessing the Web Interface

Once the application is running, open your web browser and navigate to:

**http://localhost:5555**

The web interface provides:
- Add new yahrzeit entries
- View all saved yahrzeits
- Edit existing entries
- Delete entries

## Troubleshooting

### Connection Failed
- Verify MySQL is running: `mysql -u root -p`
- Check firewall settings
- Verify connection string parameters

### Database Creation Issues
- Ensure user has CREATE DATABASE permissions
- Try creating the database manually using MySQL Workbench or command line

### Port Already in Use
- If port 5555 is in use, change it in `YahrzeitWebServer.cs`:
  ```csharp
  private readonly int _port = 5555; // Change to another port
  ```
