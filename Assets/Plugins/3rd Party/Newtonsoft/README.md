# com.unity.nuget.newtonsoft-json
Unity Package for [Newtonsoft's JSON library] (https://www.newtonsoft.com/json)

# Version

This package includes the DLL version of JSON.Net

Version: 12.0.301

# Add to your project with git url (2019.3+)

Open the manifest.json for your project and add the following entry to your list of dependencies

```json
"com.unity.nuget.newtonsoft-json": "git@github.cds.internal.unity3d.com:unity/com.unity.nuget.newtonsoft-json.git",
```

Example:
```json
{
  "dependencies": {
    "com.unity.nuget.newtonsoft-json": "git@github.cds.internal.unity3d.com:unity/com.unity.nuget.newtonsoft-json.git",
    "com.unity.ads": "2.0.8",
    "com.unity.analytics": "3.2.2",
    "com.unity.collab-proxy": "1.2.15",
    ...
    }
 }
```

# Add to your project with version

Open the manifest.json for your project and add the following entry to your list of dependencies with the desired version

```json
"com.unity.nuget.newtonsoft-json": "1.1.2",
```

Example:

```json
{
  "dependencies": {
    "com.unity.nuget.newtonsoft-json": "1.1.2",
    "com.unity.ads": "2.0.8",
    "com.unity.analytics": "3.2.2",
    "com.unity.collab-proxy": "1.2.15",
    ...
    }
 }
 ```

 # Add to your Package as a dependency

 Open the package.json for your project and add the following entry to the dependencies list with the desired Version

```json
"com.unity.nuget.newtonsoft-json": "1.1.2"
```

Example:
```json
 "dependencies": {
		"com.unity.nuget.newtonsoft-json": "1.1.2"
	},
```


# Using the package

 In the target package, modify the asmdef to include the `Newtonsoft.Json.dll` under the Assembly References section
 in the asmdef inspector.  The section will not appear until the Override References toggle above is toggled on.
 Once that is done, your package will have full access to Newtonsoft Json apis.
