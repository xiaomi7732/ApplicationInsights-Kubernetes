## Setup Application Insights

1. Prepare a file, `secrets.json` for example, with content:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "Your_connection_string"
  }
}
```

1. Apply the secrets:

```shell
type ./secrets.json | dotnet user-secrets set
```

1. Verify the secrets:

```shell
dotnet user-secrets list
```