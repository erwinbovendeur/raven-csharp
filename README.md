Usage 
=====
Add a reference to both SharpRaven.Core, and a platform specific SharpRaven assembly. Regular .NET projects use SharpRaven.NET.

Bootstrap Sentry by calling:

```csharp
Sentry.Init();
Sentry.Dsn = "http://public:secret@example.com/project-id";
```

or instantiate a client with your DSN:
```csharp
var ravenClient = new RavenClient("http://public:secret@example.com/project-id");
```

Capturing Exceptions
--------------------
```csharp
try
{
    int i2 = 0;
    int i = 10 / i2;
}
catch (Exception e)
{
    Sentry.CaptureException(e);
}
```

or use the instantiated client:
```csharp
    ravenClient.CaptureException(e);
```

Logging Non-Exceptions
----------------------
You can capture a message without being bound by an exception:

```csharp
ravenClient.CaptureMessage("Hello World!");
```

Additional Data
---------------
The capture methods allow you to provide additional data to be sent with your request. CaptureException supports both the
`tags` and `extra` properties, and CaptureMessage additionally supports the `level` property.

The full argument specs are:

```csharp
CaptureException(Exception e, IDictionary<string, string> tags = null, object extra = null)
CaptureMessage(string message, ErrorLevel level = ErrorLevel.info, Dictionary<string, string> tags = null, object extra = null)
```

ASP.NET Unhandled exception logging
-----------------------------------
If you want automatic logging of unhandled exception in your ASP.NET project, add the following handler to your web.config. Remember, for this to work, you must initialize Sentry and set the DSN.

```
<system.webServer>
  <modules>
    <add name="ErrorLog" type="SharpRaven.Http.SentryHttpModule, SharpRaven" preCondition="managedHandler" />
  </modules>
</system.webServer>
```

Mobile/universal apps
---------------------
In general, unhandled exceptions will end the application. Before this happens, SharpRaven will save a copy of the exception to local storage, and submit all saved exceptions the next time your application is started and a network connection is available.

Get it!
-------
You can clone and build SharpRaven yourself, but for those of us who are happy with prebuilt binaries, there's [a NuGet package](https://www.nuget.org/packages/SharpRaven). Note: the prebuilt binaires differ from the binaries in this repository. For this repository, no binaries are available (yet).

Resources
---------
* [Build Status](http://teamcity.codebetter.com/project.html?projectId=project344&tab=projectOverview) (requires registration)
* [Code](http://github.com/erwinbovendeur/raven-csharp)
* [Mailing List](https://groups.google.com/group/getsentry)
* [IRC](irc://irc.freenode.net/sentry) (irc.freenode.net, #sentry)
