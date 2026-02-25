//==================================================
// 
//  Copyright 2022 Siemens Digital Industries Software
//
//==================================================




using System;
using System.Collections;

using Teamcenter.ClientX;
using Teamcenter.Schemas.Soa._2006_03.Exceptions;

// Include the Data Management Service Interface
using Teamcenter.Services.Strong.Core;

// Input and output structures for the service operations
// Note: the different namespace from the service interface
using Teamcenter.Services.Strong.Core._2006_03.DataManagement;
using Teamcenter.Services.Strong.Core._2007_01.DataManagement;
using Teamcenter.Services.Strong.Core._2008_06.DataManagement;

using Teamcenter.Soa.Client.Model;
using Teamcenter.Soa.Exceptions;


namespace Teamcenter.RuntimeBO
{

    /**
     * Perform different operations in the DataManamentService
     *
     */
    public class DataManagement
    {

        /**
         * Create an instance of runtime business object
         *
         */
        public void createRuntimeBO()
        {
            try
            {
                DataManagementService dmService = DataManagementService.getService(Session.getConnection());
                CreateIn[] input = new CreateIn[1];
                input[0].ClientId = "SampleRuntimeBOclient";
                input[0].Data.BoName = "SRB9runtimebo1";
                input[0].Data.StringProps["srb9StringProp"] = "MySampleRuntimeBO";
                input[0].Data.IntProps["srb9IntegerProperty"] = 42;

                // *****************************
                // Execute the service operation
                // *****************************
                CreateResponse newObjs = dmService.CreateObjects(input);

            }
            catch (ServiceException e)
            {
                System.Console.Out.WriteLine(e.Message);
            }

        }
    }
}