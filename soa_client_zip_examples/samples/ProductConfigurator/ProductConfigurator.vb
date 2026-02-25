Imports System.Configuration
Imports Cfg0.Services.Strong.Configurator.ConfiguratorManagementService
Imports Teamcenter.Soa.Client.Model.Strong.Cfg0ProductItem
Imports Teamcenter.Soa.Client.Model.Strong.Cfg0ConfiguratorPerspective



Module ProductConfigurator

    Sub Main()
        'Get the serverhost, user and password from settings
        Dim serverHost As String = "http://10.134.152.146:7001/tc"
        Dim user As String = "tcadmin"
        Dim password As String = "tcadmin"

        Dim sess As ClientX.Session = New ClientX.Session(serverHost, user, password)
        sess.Login()
        ConfiguratorManagementUtil.Initialize(sess)
        Dim productItem As Teamcenter.Soa.Client.Model.Strong.Cfg0ProductItem = ConfiguratorManagementUtil.findItem(sess, "030989")
        Dim perspective As Teamcenter.Soa.Client.Model.Strong.Cfg0ConfiguratorPerspective = ConfiguratorManagementUtil.getConfigPerspective(productItem)
        Dim response As Cfg0.Services.Strong.Configurator._2022_06.ConfiguratorManagement.GetVariablityResponse = ConfiguratorManagementUtil.GetVariability(perspective, sess)

        System.Console.WriteLine("Ending")


    End Sub

End Module
