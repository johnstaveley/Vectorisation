# Integration Tests Configuration

## Overview
The integration tests use a centralized configuration system with the following components:

1. **appsettings.json** - Contains the WebServerUrl setting
2. **TestConfiguration** - Singleton class that loads and provides access to configuration values
3. **DefaultHttpClientFactory** - Creates HttpClient instances with the configured base URL

## Configuration File (appsettings.json)

The `appsettings.json` file contains the web server URL:

```json
{
  "TestSettings": {
    "WebServerUrl": "http://localhost:5000"
  }
}
```

## Usage

### Changing the Web Server URL

To point tests to a different server, simply update the `WebServerUrl` value in `appsettings.json`:

```json
{
  "TestSettings": {
    "WebServerUrl": "http://localhost:8080"
  }
}
```

or for a remote server:

```json
{
  "TestSettings": {
    "WebServerUrl": "https://myserver.example.com"
  }
}
```

### Environment-Specific Configuration

You can also create environment-specific configuration files:
- `appsettings.Development.json`
- `appsettings.Staging.json`
- `appsettings.Production.json`

### Test Implementation

All test classes use the `DefaultHttpClientFactory` which automatically configures the `HttpClient` with the base URL:

```csharp
public class MyTests
{
    private readonly HttpClient _client;

    public MyTests()
    {
        _client = new DefaultHttpClientFactory().CreateClient();
    }
    
    // Tests automatically use the configured base URL
}
```

### Direct Access to Configuration

If you need to access the WebServerUrl directly in your tests:

```csharp
var url = TestConfiguration.Instance.WebServerUrl;
```

## Benefits

1. **Centralized Configuration** - Change the URL in one place for all tests
2. **Environment Flexibility** - Easy to switch between local, staging, and production environments
3. **Singleton Pattern** - Configuration is loaded once and reused
4. **Type Safety** - Configuration errors are caught at startup, not during test execution
5. **Modern .NET Standard** - Uses Microsoft.Extensions.Configuration which is the standard for .NET 5+

## Notes

- The configuration is loaded lazily on first access
- If the `WebServerUrl` is missing from appsettings.json, an `InvalidOperationException` is thrown
- The appsettings.json file is automatically copied to the test output directory during build
- You can override settings using environment variables or other configuration sources if needed
