//==================================================
// 
//  Copyright 2012 Siemens Product Lifecycle Management Software Inc. All Rights Reserved.
//
//==================================================

using System;
using System.Collections;

using Teamcenter.ClientX;
using Teamcenter.Schemas.Soa._2006_03.Exceptions;


// Include the Vendor Management Service Interface

using Teamcenter.Services.Strong.Vendormanagement;
using Teamcenter.Services.Strong.Vendormanagement._2007_06.VendorManagement;


using Teamcenter.Soa.Client.Model;
using Teamcenter.Soa.Exceptions;

namespace Teamcenter.Vendor
{

    /**
     * Perform different operations in the VendorManagementService
     *
     */
    public class VendorManagement
    {

        /** TEST - 1
        * This test Creates or updates a group of vendor, vendor revisions and vendor roles.
        * Also creates a Vendor Role attaches to the vendor
        * This test also creates a Revsion B to the same Vendor
        *  Service Tested: createOrUpdateVendors
        * Input Parameters are :
        *    itemId         - Id of Vendor to be created
        *    revId          - Id of VendorRevision to be created
        *    name           - Name of Vendor to be created
        *    description    - Description of Vendor
        *    certifiStatus  - Certification status of Vendor
        *    vendorStatus   - Approval status of Vendor
        *    roleType       - Role type for Vendor(Supplier,Distributor,or Manufacturer)
        */ 
        
        
        public void createVendors()
        
        {
            Console.WriteLine("");
            Console.WriteLine("This is createVendors service");
            
            Teamcenter.Soa.Client.Model.StrongObjectFactoryVendormanagement.Init();
            
            // Get the service stub
            VendorManagementService vmService = VendorManagementService.getService(Session.getConnection());


            VendorProperties[] venProps = new VendorProperties[1];
            VendorProperties venProperty = new VendorProperties();

            venProperty.ClientId = "AppX-Test";

            Console.WriteLine("Please enter VendorId :");
            venProperty.ItemId = Console.ReadLine();

            Console.WriteLine("Please enter VendorName :");
            venProperty.Name = Console.ReadLine();


            venProperty.Type = "Vendor";

            Console.WriteLine("Please enter VendorRevisionId :");
            venProperty.RevId = Console.ReadLine();

            venProperty.Description = "This is net vendor";


            Console.WriteLine("Please enter VendorRole Type :");
            venProperty.RoleType = Console.ReadLine();


            Console.WriteLine("Please enter Vendor Certification status(Gold etc) :");
            venProperty.CertifiStatus = Console.ReadLine();

            Console.WriteLine("Please enter Vendor status(Approved/Rejected etc) :");
            venProperty.VendorStatus = Console.ReadLine();

            venProps[0] = venProperty;


            CreateVendorsResponse response = vmService.CreateOrUpdateVendors(venProps, null, "");

        }

        /** TEST - 2
        * This Test  Creates or updates a group of bidPackage, bidPackage revisions
        * Creates two BidPackage Revsions A and B
        *  Service Tested: createOrUpdateBidPackages
        *  Input Parameters are :
        *    itemId      -  Id of BidPackage to be created
        *    revId       -  Id of BidPackageRevision to be created
        *    name        -  Name  of BidPackage to be created
        */

        public CreateBidPacksOutput[] createBidPackages()
        
        {

            Console.WriteLine("");
            Console.WriteLine("This is createBidPackages service");
            
            Teamcenter.Soa.Client.Model.StrongObjectFactoryVendormanagement.Init();

            // Get the service stub
            VendorManagementService vmService = VendorManagementService.getService(Session.getConnection());


            BidPackageProps[] bidProps = new BidPackageProps[1];
            BidPackageProps bidProperty = new BidPackageProps();

            bidProperty.ClientId = "AppX-Test";

            Console.WriteLine("Please enter BidPackageId :");           
            bidProperty.ItemId = Console.ReadLine();

            Console.WriteLine("Please enter BidPackageName :");
            bidProperty.Name   = Console.ReadLine();

            Console.WriteLine("Please enter BidPackage Revision Id :");
            bidProperty.RevId  = Console.ReadLine();

            bidProperty.Type     = "BidPackage";

            bidProps[0] = bidProperty;


            CreateBidPacksResponse response = vmService.CreateOrUpdateBidPackages(bidProps, null, "");
            return response.Output;

        }

        /** TEST - 3
         * This Test Creates or updates a group of bidpackage lineitems and associates properties
         *  Service Tested: createOrUpdateLineItems
         *  Input Parameters are :
         *    lineitemname              - lineitem name to be created
         *    lineitemdesc              - lineitem description
         *    itemId                    - BidPackage id to which lineitem is to be attached
         *    revId                     - BidPackageRevision id to which lineitem is to be attached
         *    partid                    - Part id which is to  be attached to lineitem
         *    viewtype                  - PSView Type to be associated with lineitemconfigcontext
         *    quantity                  - Quantity to be created for lineitems
         *    revRule                   - Revision rule to be associated with ineitemconfigcontext
         *    varRule                   - Variant rule to be associated with ineitemconfigcontext
         *    closureRule               - Closure rule to be associated with ineitemconfigcontext
         *    liccname                  - Name for lineitemconfigcontext to be created
         *    liccdesc                  - Description for the lineitemconfigcontext
         *    name                      - Name for BidPackage
         *    description               - Description for BidPackage
         *    quote                     - Quote Tag to be associated with lineitem
         */

        public void createLineItems()
        
        {

            Console.WriteLine("");
            Console.WriteLine("This is createLineItems service");

            Teamcenter.Soa.Client.Model.StrongObjectFactoryVendormanagement.Init();

            // Get the service stub
            VendorManagementService vmService = VendorManagementService.getService(Session.getConnection());


            LineItemProps[] lineProps = new LineItemProps[1];
            LineItemProps lineProperty = new LineItemProps();            

            

            Console.WriteLine("Please enter LineItemName :");
            lineProperty.Name = Console.ReadLine();

            Console.WriteLine("Please enter LineItem Description :");
            lineProperty.Description = Console.ReadLine();

            Console.WriteLine("Please enter Part to be associated with LineItem :");
            lineProperty.Partid = Console.ReadLine();         



            lineProperty.Quantity = 2;
            lineProperty.Quote = null;

            Console.WriteLine("Please enter  LineItem Configuration Context Name:");
            lineProperty.Liccname = Console.ReadLine();

            lineProperty.Liccdesc = "Net Licc";
            lineProperty.Partid   = "";
            lineProperty.ClosureRule = "";
            lineProperty.RevRule = "";
            lineProperty.VarRule = "";
            lineProperty.Viewtype = "";

            lineProps[0] = lineProperty;


            BidPackageProps[] bidProps = new BidPackageProps[1];
            BidPackageProps bidProperty = new BidPackageProps();

            bidProperty.ClientId = "AppX-Test";

            Console.WriteLine("Please enter BidPackageId for associating lineitem :");
            bidProperty.ItemId = Console.ReadLine();
           
            bidProperty.Name = "";

            Console.WriteLine("Please enter BidPackage Revision Id :");
            bidProperty.RevId = Console.ReadLine();

            bidProperty.Type = "BidPackage";

            bidProps[0] = bidProperty;

            CreateBidPacksResponse bresponse = vmService.CreateOrUpdateBidPackages(bidProps, null, "");
            

            Teamcenter.Soa.Client.Model.ServiceData lresponse = vmService.CreateOrUpdateLineItems(lineProps,bresponse.Output[0].BidPackageRev);


        }

        /** TEST - 4
         *  This Test Deletes VendorRoles associated with a VendorRevision
         *  Service Tested: deleteVendorRoles
         *  Input Parameters are :
         *    itemId    - Vendor id  to which VendorRole is attached
         *    revId     - VendorRevision id  to which VendorRole is attached
         *    roleType  - VendorRole type
         */

        public void deleteVendorRoles()
        //       throws ServiceException
        {
            Console.WriteLine("");
            Console.WriteLine("This is deleteVendorRoles service");

            Teamcenter.Soa.Client.Model.StrongObjectFactoryVendormanagement.Init();

            // Get the service stub
            VendorManagementService vmService = VendorManagementService.getService(Session.getConnection());


            VendorProperties[] venProps = new VendorProperties[1];
            VendorProperties venProperty = new VendorProperties();

            venProperty.ClientId = "AppX-Test";

            Console.WriteLine("Please enter VendorId :");
            venProperty.ItemId = Console.ReadLine();           


            venProperty.Type = "Vendor";

            Console.WriteLine("Please enter VendorRevisionId :");
            venProperty.RevId = Console.ReadLine();            


            Console.WriteLine("Please enter VendorRole Type :");
            venProperty.RoleType = Console.ReadLine();
            

            venProps[0] = venProperty;


            Teamcenter.Soa.Client.Model.ServiceData response = vmService.DeleteVendorRoles(venProps);

        }

        /** TEST - 5
         * This Test Deletes Vendors and associated VendorRevisions,VendorRoles
         *  Service Tested: deleteVendors
         *  Input Parameters are :
         *    itemId    -  Vendor id to be deleted
         *    revId     -  VendorRevision id to be deleted
         */

        public void deleteVendors()
        //       throws ServiceException
        {
            Console.WriteLine("");
            Console.WriteLine("This is deleteVendors service");

            Teamcenter.Soa.Client.Model.StrongObjectFactoryVendormanagement.Init();

            // Get the service stub
            VendorManagementService vmService = VendorManagementService.getService(Session.getConnection());


            VendorProperties[] venProps = new VendorProperties[1];
            VendorProperties venProperty = new VendorProperties();

            venProperty.ClientId = "AppX-Test";

            Console.WriteLine("Please enter VendorId :");
            venProperty.ItemId = Console.ReadLine();


            venProperty.Type = "Vendor";

            Console.WriteLine("Please enter VendorRevisionId :");
            venProperty.RevId = Console.ReadLine();           


            venProps[0] = venProperty;


            Teamcenter.Soa.Client.Model.ServiceData response = vmService.DeleteVendors(venProps);

        }

        /** TEST - 6
         * This  Tests the  createOrUpdateVendorParts Service for CommercialPart and ManufacturerPart
         *
         *  Input Parameters are :
         *    partId              - Id for part to be created
         *    name                - Name for the part object to be created
         *    type                - Part Type to be created(Only CommercialPart
         *                          or ManufacturerPart are valid)
         *    revId               - Part Revision Id for create
         *    description         - Description for the part object to be created
         *    vendorid            - Vendor Id to be associated with Part
         *                          vendorid is optional for CommercialPart
         *    commercialpartid    - CommercialPart Id  to be associated
         *                          with ManufacturerPart(Mandatory)
         *    commercialpartrevid - CommercialPartrevision Id  to be associated
         *                          with ManufacturerPart(Mandatory)
         *    isDesignReq         - flag value to decide if design required
         *    uom                 - Unit of measure tag value
         *    makebuy             - makebuy value for Part
         */

        public void createParts()
       
        {
            Console.WriteLine("");
            Console.WriteLine("This is createParts service");
            Console.WriteLine("");
            Console.WriteLine("createParts service can create CommercialPart and ManufacturerPart");
            Console.WriteLine("This sample will create CommercialPart");

            Teamcenter.Soa.Client.Model.StrongObjectFactoryVendormanagement.Init();

            // Get the service stub
            VendorManagementService vmService = VendorManagementService.getService(Session.getConnection());

            VendorPartProperties[] partProps  = new VendorPartProperties[1];          

           
            VendorPartProperties partProperty = new VendorPartProperties();


            partProperty.ClientId = "AppX-Test";

            Console.WriteLine("Please enter PartId :");
            partProperty.PartId = Console.ReadLine();

            Console.WriteLine("Please enter Part Name :");
            partProperty.Name = Console.ReadLine();

            Console.WriteLine("Type is CommercialPart(Only CommercialPart/ManufacturerPart are Valid):");
            partProperty.Type = "CommercialPart";

            Console.WriteLine("Please enter Part Revision id:");
            partProperty.RevId = Console.ReadLine();

            Console.WriteLine("Please enter Part Description:");
            partProperty.Description = Console.ReadLine();

            Console.WriteLine("Please enter Vendorid:(Mandatory for ManufacturerPart)" );
            partProperty.Vendorid = Console.ReadLine();

            partProperty.Uom = null;
            partProperty.Makebuy = 2;   // Default value for make/by is 2
            partProperty.IsDesignReq = true;



            if (partProperty.Type.Equals("ManufacturerPart"))
            {
                Console.WriteLine("Please enter CommercialPartid:(Mandatory for ManufacturerPart)");
                partProperty.Commercialpartid = Console.ReadLine();

                Console.WriteLine("Please enter CommercialPartRevision id:(Mandatory for ManufacturerPart)");
                partProperty.Commercialpartrevid = Console.ReadLine();
            }



            partProps[0] = partProperty;

            CreateVendorPartsResponse response = vmService.CreateOrUpdateVendorParts(partProps, null, "");

                       

        }




    }
}

   