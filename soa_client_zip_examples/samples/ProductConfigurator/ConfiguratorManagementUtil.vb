Imports System.Text
Imports User = Teamcenter.Soa.Client.Model.Strong.User
Imports Cfg0.Services.Internal.Strong.Configurator
'Imports Teamcenter.Services.Strong.Core
Imports Item = Teamcenter.Soa.Client.Model.Strong.Item
Imports ModelObject = Teamcenter.Soa.Client.Model.ModelObject
Imports Teamcenter.Soa.Client.Model.Strong
Imports Teamcenter.Soa.Client.Model
Imports Teamcenter.Soa.Common
Imports SessionService = Teamcenter.Services.Strong.Core.SessionService
Imports Teamcenter.Services.Strong.Core._2007_01.DataManagement.GetItemFromIdPref
Imports Teamcenter.Services.Strong.Core._2008_06.DataManagement
Imports Teamcenter.Services.Strong.Core._2009_10.DataManagement
Imports DataManagementService = Teamcenter.Services.Strong.Core.DataManagementService
Imports CreateIn = Teamcenter.Services.Strong.Core._2008_06.DataManagement.CreateIn
Imports CreateInput = Teamcenter.Services.Strong.Core._2008_06.DataManagement.CreateInput
Imports CreateOut = Teamcenter.Services.Strong.Core._2008_06.DataManagement.CreateOut
Imports CreateResponse = Teamcenter.Services.Strong.Core._2008_06.DataManagement.CreateResponse
Imports CreateRelationsResponse = Teamcenter.Services.Strong.Core._2006_03.DataManagement.CreateRelationsResponse
Imports CreateRelationsOutput = Teamcenter.Services.Strong.Core._2006_03.DataManagement.CreateRelationsOutput
Imports Relationship = Teamcenter.Services.Strong.Core._2006_03.DataManagement.Relationship


Public Class ConfiguratorManagementUtil

    Public Shared Function Login(host As String, user As String, password As String) As Session
        Dim sess As Session = New Session(host, user, password)
        sess.Login()
        Return sess
    End Function


    ''' Adds a type and properties to the given property policy.
    Private Shared Function AddToPolicy(policy As ObjectPropertyPolicy, type As String, props() As String)
        Dim policyType As PolicyType = policy.GetType(type)
        If IsNothing(policyType) Then
            policyType = New PolicyType(type)
            policy.AddType(policyType)
        End If

        For Each prop As String In props
            Dim policyProp As PolicyProperty = policyType.GetProperty(prop)
            If IsNothing(policyProp) Then
                policyProp = New PolicyProperty(prop)
                policyProp.SetModifier(PolicyProperty.WITH_PROPERTIES, True)
                policyType.AddProperty(policyProp)
            End If
        Next
    End Function
    ''' <summary>
    ''' Initializes the SOA types and property policy.
    ''' </summary>
    ''' <param name="sess">The SOA session.</param>
    ''' <remarks></remarks>
    Public Shared Function Initialize(sess As Session)
        'Ensure types are registered.
        Teamcenter.Soa.Client.Model.StrongObjectFactory.Init()
        Teamcenter.Soa.Client.Model.StrongObjectFactoryCfg0configurator.Init()

        Dim session As Teamcenter.Soa.Client.Model.Strong.Session

        ' Set the object policy.
        Dim policy As New ObjectPropertyPolicy()
        AddToPolicy(policy, "WorkspaceObject", {"object_name", "object_desc", "creation_date"})
        AddToPolicy(policy, "Item", {"item_id"})
        AddToPolicy(policy, "ItemRevision", {"item_revision_id"})
        AddToPolicy(policy, "Cfg0AbsFamily", {"cfg0ValueDataType", "cfg0IsMultiselect", "cfg0HasFreeFormValues"})
        AddToPolicy(policy, "Cfg0AbsConfiguratorWSO", {"cfg0Sequence"})
        Dim cfg0ConfiguratorPerspectiveProperties() As String = New String() {"cfg0RevisionRule", "cfg0VariantCriteria", "cfg0ProductItems", "cfg0Models", "cfg0SavedVariantRules", "cfg0ModelFamilies", "cfg0OptionFamilies", "cfg0OptionValues", "cfg0ModelFamilies", "cfg0FamilyGroups", "cfg0ExcludeRules", "cfg0IncludeRules", "cfg0DefaultRules", "cfg0RuleSetCompileDate", "cfg0RuleSetEffectivity", "cfg0PrivateFamilies", "cfg0PrivateValues", "cfg0PublicFamilies", "cfg0PublicValues"}
        AddToPolicy(policy, "Cfg0ConfiguratorPerspective", cfg0ConfiguratorPerspectiveProperties)
        Dim cfg0ProductItemProperties() As String = New String() {"cfg0ConfigPerspective", "cfg0PosBiasedVariantAvail", "fnd0VariantNamespace"}
        AddToPolicy(policy, "Cfg0ProductItem", cfg0ProductItemProperties)


        SessionService.getService(sess.connection).SetObjectPropertyPolicy(policy)
    End Function


    Public Shared Function findItem(ByVal sess As Session, ByVal itemId As String) As Item
        Dim getProductItemResponse As GetItemFromAttributeResponse = New GetItemFromAttributeResponse
        Dim productItem As Item = Nothing
        Try
            ' get Item for given item revision
            Dim productItemAttributeInfo() As GetItemFromAttributeInfo = New GetItemFromAttributeInfo(0) {}
            productItemAttributeInfo(0) = New GetItemFromAttributeInfo
            productItemAttributeInfo(0).ItemAttributes.Add("item_id", itemId)
            Dim productIdPref As Teamcenter.Services.Strong.Core._2007_01.DataManagement.GetItemFromIdPref = New Teamcenter.Services.Strong.Core._2007_01.DataManagement.GetItemFromIdPref()
            Dim productRevId() As String = New String(0) {}
            productRevId(0) = ""
            productItemAttributeInfo(0).RevIds = productRevId
            getProductItemResponse = DataManagementService.getService(sess.connection).GetItemFromAttribute(productItemAttributeInfo, 1, productIdPref)
            If (getProductItemResponse.ServiceData.sizeOfPartialErrors > 0) Then
                Dim getItemFailureMessage As String = String.Format("findItem failed for item having  Id %s.", itemId)
            End If

            If (getProductItemResponse.Output.Length <> 0) Then
                productItem = getProductItemResponse.Output(0).Item
            End If

        Catch ex As Exception

        End Try

        Return productItem
    End Function

    Public Overloads Shared Function getConfigPerspective(ByVal item As Item) As Cfg0ConfiguratorPerspective
        'Assuming the property policy is already set
        Dim configPerspective As Cfg0ConfiguratorPerspective = item.GetProperty("cfg0ConfigPerspective").ModelObjectValue
        Return configPerspective
    End Function

    Public Shared Function GetVariability(perspective As Cfg0ConfiguratorPerspective, sess As Session) As Cfg0.Services.Strong.Configurator._2022_06.ConfiguratorManagement.GetVariablityResponse
        ' GEt the configurator management service
        Dim cfgService As Cfg0.Services.Strong.Configurator.ConfiguratorManagementService = Cfg0.Services.Strong.Configurator.ConfiguratorManagementService.getService(sess.connection)

        Dim keyValuePair(0) As Cfg0.Services.Strong.Configurator._2022_06.ConfiguratorManagement.KeyValuePair

        Return cfgService.GetVariability(perspective, keyValuePair)
    End Function


End Class
