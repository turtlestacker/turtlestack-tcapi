
This sample demonstrates the basic functionality of the Teamcenter Services.


Before running the sample application, you must have a Teamcenter Web Tier server
and Pool Manager up and running and your test database must contain the sample runtime business object template.  This
template can be found at .../soa_clients/net/samples/RuntimeBO/sampleruntimebo.  Use the Business Modeler IDE 
to import the template, build the custom extension library and deploy to your test database.  Detailed instructions for importing 
a template, working with extensions and deploying changes to a database are available in Business Modeler IDE Help.


To build this project from Visual Studio 2019 (16.7.10).


1. Load Visual Studio from this command prompt   

2. Open the project from the Open dialog ( File --> Open --> Project... )
   Browse to .../soa_client/net/samples/RuntimeBO/RuntimeBO.csproj

3. Build the project.

4. Execute the client application
   To connect to a server other than http://localhost:7001/tc, change this URI
   on Project Properties dialog ( Project --> Properties). On the Debug tab 
   modify the 'Command line arguments' field i.e. add '-host http://server:port/tc'.

Running in TCCS mode
1. To run in TCCS mode, ensure that TCCS and its configuration files are installed and TCCS is 
   already running.

2. Set the FMS_HOME to correct value and ensure TCCS native libs are in PATH on Windows or LD_LIBRARY_PATH on Unix platforms. 

3. Use tccs://<tccs_envname> (TCCS protocol and environment name) as the 
   host URI instead of http://hostname:port/tc 
    
 
 The source file RuntimeBO.cs has the main function for the application. This is the 
 best place to start browsing the code. There are several classes prefixed with 
 the name AppX (AppXCredentialManager). These classes are implementations of 
 Teamcenter Services framework interfaces. Each client application is responsible 
 for providing an implementation to these interfaces:
 
     CredentialManager       Used by the framework to re-authenticate as user.
     ExceptionHandler        Provide a mechanism for the client application to
                             handle low level exceptions in the communication framework.
     PartialErrorListener    Optional listener for notifications when a service operation
                             has returned partial errors.
     ModelEventListener      Optional listener for notification when a service operation
                             has returned ModelObject with updated property values, 
                             or when objects have been deleted from the database.
 
 The remaining classes in this sample show the use of a few of the service operations 
 to demonstrate some basic features of Teamcenter Services.
 
     Session                 This class shows how to establish a session with the
                             Teamcenter Server using the SessionService login and 
                             logout methods. A session must be established before
                             any other service operations are called.
     DataManagement          This class creates and modified Runtime Business objects
     
 Each of these examples performs the same basic steps
     1. Construct the desired service stub.
     2. Gather the data for the opeation's input arguments,
     3. Call the service operation
     4. Process the results.
     
 A few of the service operations will make use of the Change and Delete listeners.
 Under normal circomstances the ExeptionHandler and PartialErrorListner will not
 be called.
 
 
 
