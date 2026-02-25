
This sample demonstrates the basic functionality of the Teamcenter Services.


Before running the sample application, you must have a Teamcenter Web Tier server
and Pool Manager up and running.

To build this project from Visual Studio 2019 (16.7.10).

1. Set the environment variable FMS_HOME to point to the correct value for your Teamcenter installation.

2. Load Visual Studio from this command prompt   

3. Open the project from the Open dialog ( File --> Open --> Project... )
   Browse to .../soa_client/net/samples/FileManagement/FileManagement.csproj

4. Build the project.

5. Execute the client application
   To connect to a server other than http://localhost:7001/tc, change this URI
   on Project Properties dialog ( Project --> Properties). On the Debug tab 
   modify the 'Command line arguments' field i.e. add '-host http://server:port/tc'.

Running in TCCS mode
1. To run in TCCS mode, ensure that TCCS and its configuration files are installed and TCCS is 
   already running.

2. Set the FMS_HOME to correct value and ensure TCCS native libs are in PATH on Windows platforms. 

3. Use tccs://<tccs_envname> (TCCS protocol and environment name) as the 
   host URI instead of http://hostname:port/tc 
    
 The source file FMS.cs has the main function for the application. This is the 
 best place to start browsing the code. The FileManagement class performs basic 
 File Management operations,  mostly using the FileManagementUtility class provided
 as part of the SOA .NET client framework.