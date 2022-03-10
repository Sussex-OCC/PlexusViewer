# PlexusViewer
The Plexus Viewer helps the clinical workers access clinical information of a patient from GP connect.

Here is a high level overview of our architecture: 
(PS: Only the Plexus Viewer solution in this GitHub repo)

![Environment diagram](https://user-images.githubusercontent.com/96543046/150314363-975c35fd-3c8e-48ee-81bc-f6e954df060c.png)

Tech stack:
The Plexus Viewer is built on .Net 5 framework, C#, MVC & Javascript. The logging is offloaded to Azure Service Bus Topics and function apps write from those Topics to our database. The entire solution and its dependencies are hosted in our Azure infrastructure. The solution uses the Microsoft Graph api to obtain the logged in user's information from their Azure Active Directory account.

The viewer surfaces the patient data obtained from Spine and GP Connect. The Spine service provides the patient demographics which is displayed in the banner. The clinical items of the patient comes from the GP Connect services. The solution depends on the Plexus gateway, Spine gateway an GP Connect gateway services which are deployed as microservices.

PLEASE NOTE: The gateway services aren't included in this repository. So, you may not be able to build the solution.
