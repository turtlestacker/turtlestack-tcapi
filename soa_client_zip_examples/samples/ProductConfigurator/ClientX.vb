Imports Teamcenter.Soa.Client
Imports Teamcenter.Schemas.Soa._2006_03.Exceptions
Imports Teamcenter.Soa
Imports User = Teamcenter.Soa.Client.Model.Strong.User
Imports Teamcenter.Soa.Client.Model.Strong.Session
Imports Teamcenter.Services.Strong.Core
Imports Teamcenter.Soa.Exceptions
Imports System.IO
Imports Teamcenter.Services.Strong.Core._2006_03.Session

''' <summary>
''' This module implements a minimal SOA client, consisting only of the session and credential manager.  For the most
''' part, this is copied right out of the sample code in the SOA package.  The credential manager has been modified to
''' take a username and password as parameters rather than prompt for them - this is so we can pass these in from RS.
''' </summary>
''' <remarks></remarks>
Public Module ClientX
    Public Class AppXCredentialManager
        Implements CredentialManager

        Private name As String = Nothing
        Private password As String = Nothing
        Private group As String = ""
        Private role As String = ""
        Private discriminator As String = "SoaAppX"

        Public Sub New(username As String, password As String)
            Me.name = username
            Me.password = password
        End Sub

        Public Sub SetGroupRole(group As String, role As String) _
            Implements CredentialManager.SetGroupRole
            Me.group = group
            Me.role = role
        End Sub

        Public Sub SetUserPassword(user As String, password As String, discriminator As String) _
            Implements CredentialManager.SetUserPassword
            Me.name = user
            Me.password = password
            Me.discriminator = discriminator
        End Sub

        Public ReadOnly Property CredentialType() As Integer _
            Implements CredentialManager.CredentialType
            Get
                Return SoaConstants.CLIENT_CREDENTIAL_TYPE_STD
            End Get
        End Property

        Public Function GetCredentials(invalidUser As InvalidCredentialsException) As String() _
            Implements CredentialManager.GetCredentials
            Console.WriteLine(invalidUser.Message)
            Return PromptForCredentials()
        End Function

        Public Function GetCredentials(invalidUser As InvalidUserException) As String() _
            Implements CredentialManager.GetCredentials
            If (name = Nothing) Then
                Return PromptForCredentials()
            End If

            Return {name, password, group, role, discriminator}
        End Function

        Public Function PromptForCredentials() As String()
            If (name <> Nothing) Then
                Return {name, password, group, role, discriminator}
            End If

            Try
                Console.WriteLine("Please enter user credentials (return to quit):")
                Console.Write("User Name: ")
                name = Console.ReadLine()

                If (name.Length = 0) Then
                    Throw New CanceledOperationException("")
                End If

                Console.Write("Password:  ")
                password = Console.ReadLine()
            Catch ex As IOException
                Dim message As String = "Failed to get the name and password.\n" + ex.Message
                Console.WriteLine(message)
                Throw New CanceledOperationException(message)
            End Try

            Return {name, password, group, role, discriminator}
        End Function
    End Class

    Public Class Session
        Private conn As Connection
        Private credentialManager As AppXCredentialManager
        Private loggedInUser As User

        Public Sub New(host As String, username As String, password As String)
            credentialManager = New AppXCredentialManager(username, password)
            Dim proto As String = Nothing
            Dim envNameTccs As String = Nothing

            If (host.StartsWith("http")) Then
                proto = SoaConstants.HTTP
            ElseIf (host.StartsWith("tccs")) Then
                proto = SoaConstants.TCCS
                Dim envNameStart As Integer = host.IndexOf("/") + 2
                envNameTccs = host.Substring(envNameStart, host.Length - envNameStart)
            End If

            conn = New Connection(host, New System.Net.CookieCollection, credentialManager, SoaConstants.REST, proto, False)

            If (proto = SoaConstants.TCCS) Then
                connection.SetOption(connection.TCCS_ENV_NAME, envNameTccs)
            End If

            ' TODO: Add the rest of the listeners/handlers here...
        End Sub

        Public ReadOnly Property connection As Connection
            Get
                Return conn
            End Get
        End Property

        Public ReadOnly Property user As User
            Get
                Return loggedInUser
            End Get
        End Property

        Public Function Login() As User
            loggedInUser = Nothing
            Dim sessionService As SessionService = SessionService.getService(conn)
            Try
                Dim credentials As String() = credentialManager.PromptForCredentials()
                While (True)
                    Try
                        Dim resp As LoginResponse
                        resp = sessionService.Login(credentials(0), credentials(1), credentials(2), credentials(3), "", credentials(4))
                        loggedInUser = resp.User
                        Return resp.User
                    Catch ex As InvalidCredentialsException
                        credentials = credentialManager.GetCredentials(ex)
                    End Try
                End While
            Catch ex As CanceledOperationException
                ' ignore
            End Try

            Return Nothing
        End Function

        Public Sub Logout()
            Dim sessionService As SessionService = sessionService.getService(conn)
            Try
                sessionService.Logout()
            Catch ex As ServiceException
                ' ignore
            End Try
        End Sub
    End Class
End Module
