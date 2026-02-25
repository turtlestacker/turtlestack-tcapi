
This sample demonstrates the basic functionality of the Teamcenter VendorManagement Services.



Before running the sample application, you must have a Teamcenter Web Tier server
and Pool Manager up and running.

To build this project from Visual Studio 2019 (16.7.10).

1. Load the project
   Open the Open dialog ( File --> Open --> Project... )
   Browse to .../soa_clients/net/samples/VendorManagement/VendorManagement.csproj
   
2. Compile the project

3. Execute the client application
   To connect to a server other than http://localhost:7001/tc, change this URI
   on Project Properties dialog ( Project --> Properties). On the Debug tab 
   modify the In the 'Command line arguments' field add '-host http://server:port/tc'.
 
 The source file VendorManagementMain.cs has the main function for the application. This is the 
 best place to start browsing the code. There are serveral classes prefixed with 
 the name AppX (AppXCredentialManager), these classes are implemenations of 
 Teamcenter Services framework interfaces. Each client application is responsible 
 for providing an implemenation to these interfaces:
 
     CredentialManager       Used by the framework to re-authenticate as user.
     ExceptionHandler        Provide a mechanism for the client application to
                             handle low level exception in the communication framework.
     PartialErrorListener    Optional listener for notification when a service operation
                             has returned partial errors.
     ChangeListener          Optional listener for notification when a service operation
                             has returned ModelObject with updated property values.
     DeleteListener          Optional listener for notification when a service operation
                             has returned ModelObject that have been deleted from
                             the database and from the client data model.
 
 The remaining classes in this sample show the use of a few of the service operations 
 to demonstrate some basic features of Teamcenter Services.
 
     Session                 This class shows how to establish a session with the
                             Teamcenter Server using the SessionService login and 
                             logout methods. A session must be established before
                             any other service operation are called.     
     VendorManagement        This class creates, revises, and deletes a set of Vendors,BidPackages,Lineitems,VendorParts
     
 Each of these examples performs the same basic steps
     1. Construct the desired service stub.
     2. Gather the data for the opeation's input arguments,
     3. Call the service operation
     4. Process the results.
     
 A few of the service operations will make use of the Change and Delete listeners.
 Under normal circomstances the ExeptionHandler and PartialErrorListner will not
 be called.
 
 
 
