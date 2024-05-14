using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using IGS.DataServices;
using IGS.DataServices.EF;
using IGS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MI.DEGProcessor.Tests;

[TestClass]
public class WorkXmlTests
{
    private readonly DateTime receivedDate = new(2018, 4, 26);
    private          string   _testXml;

    private string TestXml
    {
        get
        {
            if (_testXml == null)
            {
                var assembly     = Assembly.GetExecutingAssembly();
                var resourceName = "MI.DEGProcessor.Tests.TestFiles.TestXmlDocument3.xml";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new StreamReader(stream);
                _testXml = reader.ReadToEnd();
            }

            return _testXml;
        }
    }

    private string TestXmlSIT
    {
        get
        {
            if (_testXml == null)
            {
                var assembly     = Assembly.GetExecutingAssembly();
                var resourceName = "MI.DEGProcessor.Tests.TestFiles.TestXmlDocumentSIT.xml";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new StreamReader(stream);
                _testXml = reader.ReadToEnd();
            }

            return _testXml;
        }
    }

    private string TestXml4
    {
        get
        {
            if (_testXml == null)
            {
                var assembly     = Assembly.GetExecutingAssembly();
                var resourceName = "MI.DEGProcessor.Tests.TestFiles.TestXmlDocument4.xml";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new StreamReader(stream);
                _testXml = reader.ReadToEnd();
            }

            return _testXml;
        }
    }

    private string TestXmlAddress
    {
        get
        {
            if (_testXml == null)
            {
                var assembly     = Assembly.GetExecutingAssembly();
                var resourceName = "MI.DEGProcessor.Tests.TestFiles.TestXmlDocument5.xml";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new StreamReader(stream);
                _testXml = reader.ReadToEnd();
            }

            return _testXml;
        }
    }

    private string TestXmlPhone
    {
        get
        {
            if (_testXml == null)
            {
                var assembly     = Assembly.GetExecutingAssembly();
                var resourceName = "MI.DEGProcessor.Tests.TestFiles.TestXmlDocument6.xml";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new StreamReader(stream);
                _testXml = reader.ReadToEnd();
            }

            return _testXml;
        }
    }

    private string TestXml7
    {
        get
        {
            if (_testXml == null)
            {
                var assembly     = Assembly.GetExecutingAssembly();
                var resourceName = "MI.DEGProcessor.Tests.TestFiles.TestXmlDocument7.xml";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new StreamReader(stream);
                _testXml = reader.ReadToEnd();
            }

            return _testXml;
        }
    }

    private string TestXml8
    {
        get
        {
            if (_testXml == null)
            {
                var assembly     = Assembly.GetExecutingAssembly();
                var resourceName = "MI.DEGProcessor.Tests.TestFiles.TestXmlDocument8.xml";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new StreamReader(stream);
                _testXml = reader.ReadToEnd();
            }

            return _testXml;
        }
    }

    private string TestXmlFlint
    {
        get
        {
            if (_testXml == null)
            {
                var assembly     = Assembly.GetExecutingAssembly();
                var resourceName = "MI.DEGProcessor.Tests.TestFiles.TestXmlDocumentFlint.xml";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new StreamReader(stream);
                _testXml = reader.ReadToEnd();
            }

            return _testXml;
        }
    }

    private string TestXmlPregnancy
    {
        get
        {
            if (_testXml == null)
            {
                var assembly     = Assembly.GetExecutingAssembly();
                var resourceName = "MI.DEGProcessor.Tests.TestFiles.TestXmlDocumentPregnancy.xml";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new StreamReader(stream);
                _testXml = reader.ReadToEnd();
            }

            return _testXml;
        }
    }

    private string TestXmlDateWithTimeZone
    {
        get
        {
            if (_testXml == null)
            {
                var assembly     = Assembly.GetExecutingAssembly();
                var resourceName = "MI.DEGProcessor.Tests.TestFiles.TestXmlDocumentDateWithTimeZone.xml";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new StreamReader(stream);
                _testXml = reader.ReadToEnd();
            }

            return _testXml;
        }
    }

    private DataServices CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<Miaca_IgsContext>().UseInMemoryDatabase($"MIACA_Igs_{Guid.NewGuid()}")
                                                                     .EnableSensitiveDataLogging()
                                                                     .Options;
        var context = new Miaca_IgsContext(options);
        var data    = new DataServices(context);

        #region Add Data

        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 1, ReasonCode = "IR1001", ProgramCode = "MAGI-U19",
                                   Priority                = 1,
                                   IneligibilityReasonText = "Individual did not apply for Health Care Coverage.",
                                   IneligibilityReason     = "The person did not apply for coverage",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 2, ReasonCode = "IR1302", ProgramCode = "MAGI-U19",
                                   Priority                = 4,
                                   IneligibilityReasonText = "Countable income exceeds income limit for your group size.",
                                   IneligibilityReason     = "The person did not meet the program financial requirements",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 3, ReasonCode = "IR1703", ProgramCode = "MAGI-U19",
                                   Priority                = 2,
                                   IneligibilityReasonText = "Individual is not a resident of Michigan.",
                                   IneligibilityReason     = "The person is not a resident of MI",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 4, ReasonCode = "IR1200", ProgramCode = "MAGI-U19",
                                   Priority                = 6,
                                   IneligibilityReasonText = "This individual does not meet the SSN requirements.",
                                   IneligibilityReason     = "The person does not meet the SSN requirements",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 5, ReasonCode = "IR1419", ProgramCode = "MAGI-U19",
                                   Priority                = 3,
                                   IneligibilityReasonText = "Individual does not meet the age requirement of under 19.",
                                   IneligibilityReason     = "The person's age is > 18", CreatedBy = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId = 6, ReasonCode = "IR1500", ProgramCode = "MAGI-U19",
                                   Priority       = 5,
                                   IneligibilityReasonText =
                                       "Individual does not qualify for full health care coverage because not a US citizen or eligible immigrant. See the \"More information about your health care coverage\" section of the notice.",
                                   IneligibilityReason =
                                       "The person is eligible for ESO because they did not attest to being a US citizen or having an eligible immigration status",
                                   CreatedBy = "sa",
                                   CreatedOn = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 7, ReasonCode = "IR2001", ProgramCode = "MAGI-PW",
                                   Priority                = 1,
                                   IneligibilityReasonText = "Individual did not apply for Health Care Coverage.",
                                   IneligibilityReason     = "The person did not apply for coverage",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 8, ReasonCode = "IR2302", ProgramCode = "MAGI-PW",
                                   Priority                = 6,
                                   IneligibilityReasonText = "Countable income exceeds income limit for your group size.",
                                   IneligibilityReason     = "The person did not meet the program financial requirements",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 9, ReasonCode = "IR2703", ProgramCode = "MAGI-PW",
                                   Priority                = 2,
                                   IneligibilityReasonText = "Individual is not a resident of Michigan.",
                                   IneligibilityReason     = "The person is not a resident of MI",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 10, ReasonCode = "IR2200", ProgramCode = "MAGI-PW",
                                   Priority                = 8,
                                   IneligibilityReasonText = "This individual does not meet the SSN requirements.",
                                   IneligibilityReason     = "The person does not meet the SSN requirements",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 11, ReasonCode = "IR2800", ProgramCode = "MAGI-PW",
                                   Priority                = 4,
                                   IneligibilityReasonText = "Individual is not a female.",
                                   IneligibilityReason     = "The person is not a female",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 12, ReasonCode = "IR2850", ProgramCode = "MAGI-PW",
                                   Priority                = 3,
                                   IneligibilityReasonText = "Individual is not pregnant.",
                                   IneligibilityReason     = "The person is not pregnant",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 13, ReasonCode = "IR2601", ProgramCode = "MAGI-PW",
                                   Priority                = 5,
                                   IneligibilityReasonText = "Individual is active HMP.",
                                   IneligibilityReason     = "The person is eligible for and is currently receiving HMP",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId = 14, ReasonCode = "IR2500", ProgramCode = "MAGI-PW",
                                   Priority       = 7,
                                   IneligibilityReasonText =
                                       "Individual did not qualify for full health care coverage because not a US citizen or eligible immigrant.",
                                   IneligibilityReason =
                                       "The person is eligible for ESO because they did not attest to being a US citizen or having an eligible immigration status",
                                   CreatedBy = "sa",
                                   CreatedOn = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 15, ReasonCode       = "IR3001",
                                   ProgramCode             = "MAGI-PCR", Priority = 1,
                                   IneligibilityReasonText = "Individual did not apply for Health Care Coverage.",
                                   IneligibilityReason     = "The person did not apply for coverage",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 16, ReasonCode       = "IR3302",
                                   ProgramCode             = "MAGI-PCR", Priority = 4,
                                   IneligibilityReasonText = "Countable income exceeds income limit for your group size.",
                                   IneligibilityReason     = "The person did not meet the program financial requirements",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 17, ReasonCode       = "IR3703",
                                   ProgramCode             = "MAGI-PCR", Priority = 2,
                                   IneligibilityReasonText = "Individual is not a resident of Michigan.",
                                   IneligibilityReason     = "The person is not a resident of MI",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 18, ReasonCode       = "IR3200",
                                   ProgramCode             = "MAGI-PCR", Priority = 6,
                                   IneligibilityReasonText = "This individual does not meet the SSN requirements.",
                                   IneligibilityReason     = "The person does not meet the SSN requirements",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId = 19, ReasonCode       = "IR3419",
                                   ProgramCode    = "MAGI-PCR", Priority = 3,
                                   IneligibilityReasonText =
                                       "Individual is not a parent or caretaker relative of someone under age 19.",
                                   IneligibilityReason = "The person does not have one or more eligible dependents",
                                   CreatedBy           = "sa",
                                   CreatedOn           = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId = 20, ReasonCode       = "IR3500",
                                   ProgramCode    = "MAGI-PCR", Priority = 5,
                                   IneligibilityReasonText =
                                       "Individual does not qualify for full health care coverage because not a US citizen or eligible immigrant. See the \"More information about your health care coverage\" section of the notice.",
                                   IneligibilityReason =
                                       "The person is eligible for ESO because they did not attest to being a US citizen or having an eligible immigration status",
                                   CreatedBy = "sa",
                                   CreatedOn = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 21, ReasonCode       = "IR4001",
                                   ProgramCode             = "MAGI-FFC", Priority = 1,
                                   IneligibilityReasonText = "Individual did not apply for Health Care Coverage.",
                                   IneligibilityReason     = "The person did not apply for coverage",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 22, ReasonCode       = "IR4703",
                                   ProgramCode             = "MAGI-FFC", Priority = 2,
                                   IneligibilityReasonText = "Individual is not a resident of Michigan.",
                                   IneligibilityReason     = "The person is not a resident of MI",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 23, ReasonCode       = "IR4200",
                                   ProgramCode             = "MAGI-FFC", Priority = 6,
                                   IneligibilityReasonText = "This individual does not meet the SSN requirements.",
                                   IneligibilityReason     = "The person does not meet the SSN requirements",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 24, ReasonCode       = "IR4426",
                                   ProgramCode             = "MAGI-FFC", Priority = 4,
                                   IneligibilityReasonText = "Individual does not meet the age requirement of under 26.",
                                   IneligibilityReason     = "The person's age is >= 26", CreatedBy = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 25, ReasonCode       = "IR4418",
                                   ProgramCode             = "MAGI-FFC", Priority = 3,
                                   IneligibilityReasonText = "Individual was not in foster care at age 18.",
                                   IneligibilityReason     = "The person was not in foster care at age 18",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId = 26, ReasonCode       = "IR4500",
                                   ProgramCode    = "MAGI-FFC", Priority = 5,
                                   IneligibilityReasonText =
                                       "Individual does not qualify for full health care coverage because not a US citizen or eligible immigrant. See the \"More information about your health care coverage\" section of the notice.",
                                   IneligibilityReason =
                                       "The person is eligible for ESO because they did not attest to being a US citizen or having an eligible immigration status",
                                   CreatedBy = "sa",
                                   CreatedOn = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 27, ReasonCode = "IR5001",
                                   ProgramCode             = "MAGI-MIChild",
                                   Priority                = 2,
                                   IneligibilityReasonText = "Individual did not apply for Health Care Coverage.",
                                   IneligibilityReason     = "The person did not apply for coverage",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 28, ReasonCode = "IR5302",
                                   ProgramCode             = "MAGI-MIChild",
                                   Priority                = 7,
                                   IneligibilityReasonText = "Countable income exceeds income limit for your group size.",
                                   IneligibilityReason     = "The person did not meet the program financial requirements",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 29, ReasonCode = "IR5703",
                                   ProgramCode             = "MAGI-MIChild",
                                   Priority                = 3,
                                   IneligibilityReasonText = "Individual is not a resident of Michigan.",
                                   IneligibilityReason     = "The person is not a resident of MI",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 30, ReasonCode = "IR5200",
                                   ProgramCode             = "MAGI-MIChild",
                                   Priority                = 9,
                                   IneligibilityReasonText = "This individual does not meet the SSN requirements.",
                                   IneligibilityReason     = "The person does not meet the SSN requirements",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 31, ReasonCode = "IR5419",
                                   ProgramCode             = "MAGI-MIChild",
                                   Priority                = 6,
                                   IneligibilityReasonText = "Individual does not meet age requirement of under 19.",
                                   IneligibilityReason     = "The person's age is > 18", CreatedBy = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 32, ReasonCode = "IR5601",
                                   ProgramCode             = "MAGI-MIChild",
                                   Priority                = 5,
                                   IneligibilityReasonText = "Individual is determined income eligible for Medicaid.",
                                   IneligibilityReason     = "The person is eligible for another program",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId = 33, ReasonCode = "IR5602",
                                   ProgramCode    = "MAGI-MIChild",
                                   Priority       = 4,
                                   IneligibilityReasonText =
                                       "Individual already enrolled in Medicare, VA Health, TriCare or Peace Corp.",
                                   IneligibilityReason = "The person is currently receiving public insurance",
                                   CreatedBy           = "sa",
                                   CreatedOn           = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId = 34, ReasonCode = "IR5002",
                                   ProgramCode    = "MAGI-MIChild",
                                   Priority       = 1,
                                   IneligibilityReasonText =
                                       "Individual is pended for income or missing citizenship and immigration status.",
                                   IneligibilityReason =
                                       "The person was pended for income or missing citizenship and immigration status",
                                   CreatedBy = "sa",
                                   CreatedOn = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId = 35, ReasonCode = "IR5500",
                                   ProgramCode    = "MAGI-MIChild",
                                   Priority       = 8,
                                   IneligibilityReasonText =
                                       "Individual does not qualify for full health care coverage because not a US citizen or eligible immigrant. See the \"More information about your health care coverage\" section of the notice.",
                                   IneligibilityReason =
                                       "The person is eligible for ESO because they did not attest to being a US citizen or having an eligible immigration status",
                                   CreatedBy = "sa",
                                   CreatedOn = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 36, ReasonCode       = "IR6001",
                                   ProgramCode             = "MAGI-HMP", Priority = 1,
                                   IneligibilityReasonText = "Individual did not apply for Health Care Coverage.",
                                   IneligibilityReason     = "The person did not apply for coverage",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 37, ReasonCode       = "IR6302",
                                   ProgramCode             = "MAGI-HMP", Priority = 7,
                                   IneligibilityReasonText = "Countable income exceeds income limit for your group size.",
                                   IneligibilityReason     = "The person did not meet the program financial requirements",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 38, ReasonCode       = "IR6703",
                                   ProgramCode             = "MAGI-HMP", Priority = 2,
                                   IneligibilityReasonText = "Individual is not a resident of Michigan.",
                                   IneligibilityReason     = "The person is not a resident of MI",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 39, ReasonCode       = "IR6200",
                                   ProgramCode             = "MAGI-HMP", Priority = 9,
                                   IneligibilityReasonText = "This individual does not meet the SSN requirements.",
                                   IneligibilityReason     = "The person does not meet the SSN requirements",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 40, ReasonCode       = "IR6851",
                                   ProgramCode             = "MAGI-HMP", Priority = 6,
                                   IneligibilityReasonText = "Individual is pregnant.",
                                   IneligibilityReason     = "The person is a pregnant female",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 41, ReasonCode       = "IR6464",
                                   ProgramCode             = "MAGI-HMP", Priority = 4,
                                   IneligibilityReasonText = "Individual does not meet the age requirement of 19 - 64.",
                                   IneligibilityReason     = "The person's age is <; 19 or > 64",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 42, ReasonCode       = "IR6602",
                                   ProgramCode             = "MAGI-HMP", Priority = 3,
                                   IneligibilityReasonText = "Individual is eligible for or enrolled in Medicare.",
                                   IneligibilityReason     = "The person is currently receiving Medicare",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId = 43, ReasonCode       = "IR6603",
                                   ProgramCode    = "MAGI-HMP", Priority = 5,
                                   IneligibilityReasonText =
                                       "One or more of the individual's dependents have not applied for or do not already have minimal essential coverage.",
                                   IneligibilityReason =
                                       "One or more of the person's dependents have not applied for coverage and do not already have coverage",
                                   CreatedBy = "sa",
                                   CreatedOn = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId = 44, ReasonCode       = "IR6500",
                                   ProgramCode    = "MAGI-HMP", Priority = 8,
                                   IneligibilityReasonText =
                                       "Individual does not qualify for full health care coverage because not a US citizen or eligible immigrant. See the \"More information about your health care coverage\" section of the notice.",
                                   IneligibilityReason =
                                       "The person is eligible for ESO because they did not attest to being a US citizen or having an eligible immigration status",
                                   CreatedBy = "sa",
                                   CreatedOn = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId = 45, ReasonCode = "IR5603",
                                   ProgramCode    = "MAGI-MIChild",
                                   Priority       = 10,
                                   IneligibilityReasonText =
                                       "Individual attested to having comprehensive healthcare/insurance coverage.",
                                   IneligibilityReason = "The person is attesting to having Comprehensive Insurance",
                                   CreatedBy           = "sa",
                                   CreatedOn           = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 46, ReasonCode       = "IR7001",
                                   ProgramCode             = "MAGI-FLT", Priority = 1,
                                   IneligibilityReasonText = "The person did not apply for coverage.",
                                   IneligibilityReason     = "The person did not apply for coverage",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 47, ReasonCode       = "IR7302",
                                   ProgramCode             = "MAGI-FLT", Priority = 2,
                                   IneligibilityReasonText = "The person did not meet the program financial requirements.",
                                   IneligibilityReason     = "The person did not meet the program financial requirements.",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 48, ReasonCode       = "IR7703",
                                   ProgramCode             = "MAGI-FLT", Priority = 3,
                                   IneligibilityReasonText = "The person is not a resident of MI.",
                                   IneligibilityReason     = "The person is not a resident of MI.",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 49, ReasonCode       = "IR7200",
                                   ProgramCode             = "MAGI-FLT", Priority = 4,
                                   IneligibilityReasonText = "The person does not meet the SSN requirements.",
                                   IneligibilityReason     = "The person does not meet the SSN requirements.",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 50, ReasonCode       = "IR7421",
                                   ProgramCode             = "MAGI-FLT", Priority = 5,
                                   IneligibilityReasonText = "The person's age is > 20.",
                                   IneligibilityReason     = "The person's age is > 20.", CreatedBy = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId = 51, ReasonCode       = "IR7500",
                                   ProgramCode    = "MAGI-FLT", Priority = 6,
                                   IneligibilityReasonText =
                                       "The person is eligible for ESO because they did not attest to being a US citizen or having an eligible immigration status.",
                                   IneligibilityReason =
                                       "The person is eligible for ESO because they did not attest to being a US citizen or having an eligible immigration status.",
                                   CreatedBy = "sa",
                                   CreatedOn = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.DenialReasons.Add(new DenialReason
                               {
                                   DenialReasonId          = 52, ReasonCode       = "IR7850",
                                   ProgramCode             = "MAGI-FLT", Priority = 7,
                                   IneligibilityReasonText = "The person is not pregnant.",
                                   IneligibilityReason     = "The person is not pregnant.",
                                   CreatedBy               = "sa",
                                   CreatedOn               = DateTime.Parse("2015-07-02 13:58:36.490")
                               });
        data.SaveChanges().Wait();

        #endregion

        return data;
    }

    [TestMethod]
    public void WorkXmlTest_TestDocumentLoad_ParseOnly()
    {
        WorkXml workXml;
        var     data = CreateDatabase();
        try
        {
            workXml = new WorkXml(data, TestXml, receivedDate, true, true, "us-east-1", true);
        }
        catch (Exception ex)
        {
            Assert.Fail("Document Load threw exception: {0}", ex.Message);
        }
    }

    [TestMethod]
    public void WorkXmlTest_TestDocumentLoad_ParseOnly8()
    {
        WorkXml workXml;
        var     data = CreateDatabase();
        try
        {
            workXml = new WorkXml(data, TestXml8, receivedDate, false, false, "us-east-1", true);
            Assert.IsTrue(workXml.Valid);

            foreach (var index in workXml.App.ApplicationIndices)
            {
                Assert.IsTrue(index.Phone.Length      <= 10);
                Assert.IsTrue(index.OtherPhone.Length <= 10);
            }
        }
        catch (Exception ex)
        {
            Assert.Fail("Document Load threw exception: {0}", ex.Message);
        }
    }

    [TestMethod]
    public async Task WorkXmlTest_TestDocumentSave()
    {
        var assembly     = Assembly.GetExecutingAssembly();
        var resourceName = "MI.DEGProcessor.Tests.TestFiles.TestXmlDocumentSIT.xml";
        var data         = CreateDatabase();

        await using var stream = assembly.GetManifestResourceStream(resourceName);
        using var       reader = new StreamReader(stream);
        var             doc    = new XmlDocument();
        doc.LoadXml(await reader.ReadToEndAsync());

        var workXml = new WorkXml(data, doc.OuterXml, DateTime.Now, false, false, "us-east-1", true);

        Assert.IsTrue(workXml.Valid, "WorkXml not valid. {0}", workXml.Exception);
        try
        {
            var info = await workXml.SaveAsync(new CancellationToken());
            Assert.IsNotNull(info);
            Assert.AreNotEqual(0, info.ApplicationTransferId);
            var testNewDenialReason =
                await data.DenialReasons.FirstOrDefaultAsync(d => d.ProgramCode.Equals("MAGI-HMP", StringComparison.OrdinalIgnoreCase) &&
                                                                  d.ReasonCode.Equals("IR8888", StringComparison.OrdinalIgnoreCase));
            Assert.IsNotNull(testNewDenialReason);
            var testNewDenialReason2 =
                await data.DenialReasons.FirstOrDefaultAsync(d => d.ProgramCode.Equals("MAGI-HMP", StringComparison.OrdinalIgnoreCase) &&
                                                                  d.ReasonCode.Equals("IR8889", StringComparison.OrdinalIgnoreCase));
            Assert.IsNotNull(testNewDenialReason2);
        }
        catch (Exception ex)
        {
            Assert.Fail("Document Save exception: {1}", ex.Message);
        }
    }

    [TestMethod]
    public async Task WorkXmlTest_TestDocumentLoad_Batch()
    {
        var assembly     = Assembly.GetExecutingAssembly();
        var resourceName = "MI.DEGProcessor.Tests.TestFiles.TestXmlDocuments.xml";
        var data         = CreateDatabase();

        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream);
        var       doc    = new XmlDocument();
        doc.LoadXml(reader.ReadToEnd());

        foreach (XmlNode node in doc.DocumentElement.ChildNodes)
        {
            var singleDoc = new XmlDocument();
            singleDoc.LoadXml(node.OuterXml);

            WorkXml             workXml = null;
            ApplicationTransfer info    = null;
            try
            {
                var edResult = new XmlDocument();
                edResult.LoadXml(singleDoc.OuterXml);

                edResult = IntegrateCDATA(edResult, "//ATXML");
                edResult = IntegrateCDATA(edResult, "//DeterminationsReasons");

                workXml = new WorkXml(data, edResult.OuterXml, DateTime.Now, false, false, "us-east-1", true);
                if (workXml.Valid)
                {
                    info = await workXml.SaveAsync(new CancellationToken());
                    Assert.IsNotNull(info);
                    Assert.AreNotEqual(0, info.ApplicationTransferId);
                }
                else
                {
                    Assert.Fail("Xml is not valid.");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail("Document Load {0} exception: {1}", workXml == null ? "" : workXml.TransferID, ex.Message);
                break;
            }
        }
    }

    private XmlDocument IntegrateCDATA(XmlDocument document, string xpath)
    {
        var xFragment = document.CreateDocumentFragment();
        xFragment.InnerXml = document.SelectSingleNode(xpath).ChildNodes[0].Value;
        document           = ReplaceFragment(document, xpath, xFragment);
        return document;
    }

    private XmlDocument ReplaceFragment(XmlDocument document, string xpath, XmlDocumentFragment fragment)
    {
        document.SelectSingleNode(xpath).RemoveAll();
        document.SelectSingleNode(xpath).AppendChild(fragment);
        return document;
    }

    [TestMethod]
    public void WorkXmlTest_TestDocument7Load()
    {
        var     data    = CreateDatabase();
        WorkXml workXml = null;
        try
        {
            workXml = new WorkXml(data, TestXml7, receivedDate, false, false, "us-east-1", true);
        }
        catch (Exception ex)
        {
            Assert.Fail("Document Load threw exception: {0}", ex.Message);
        }

        Assert.IsNotNull(workXml);
        Assert.IsTrue(workXml.Valid, "WorkXml not valid. {0}", workXml.Exception);
        var personToCheck = workXml.App.ApplicationIndices.FirstOrDefault();
        Assert.IsNotNull(personToCheck);
        Assert.AreEqual(true, personToCheck.EsienrolledIndicator);
    }

    [TestMethod]
    public void WorkXmlTest_TestDocumentSITLoad()
    {
        var     data    = CreateDatabase();
        WorkXml workXml = null;
        try
        {
            workXml = new WorkXml(data, TestXmlSIT, receivedDate, false, false, "us-east-1", true);
        }
        catch (Exception ex)
        {
            Assert.Fail("Document Load threw exception: {0}", ex.Message);
        }

        Assert.IsNotNull(workXml);
        Assert.IsTrue(workXml.Valid, "WorkXml not valid. {0}", workXml.Exception);
        Assert.IsNotNull(workXml.App);
        Assert.AreEqual(false, workXml.App.AnyoneHasHealthInsurance);
        var personToCheck = workXml.App.ApplicationIndices.FirstOrDefault();
        Assert.IsNotNull(personToCheck);
        Assert.AreEqual(true,      personToCheck.EligibleImmigrationStatus);
        Assert.AreEqual(false,     personToCheck.HasNonEsicoverage);
        Assert.AreEqual("Federal", personToCheck.InsurancePolicySourceCode);
    }

    [TestMethod]
    public void WorkXmlTest_TestDocument4Load()
    {
        var     data    = CreateDatabase();
        WorkXml workXml = null;
        try
        {
            workXml = new WorkXml(data, TestXml4, receivedDate, false, false, "us-east-1", true);
        }
        catch (Exception ex)
        {
            Assert.Fail("Document Load threw exception: {0}", ex.Message);
        }

        Assert.IsNotNull(workXml);
        var personToCheck = workXml.App.ApplicationIndices.ToArray();
        Assert.IsNotNull(personToCheck);
        //Assert.IsNull(personToCheck[0].RecentMedicalBillsIndicator);
        //Assert.AreEqual(true, personToCheck[1].RecentMedicalBillsIndicator);
        //Assert.AreEqual(true, personToCheck[2].RecentMedicalBillsIndicator);
        //Assert.IsNull(personToCheck[0].EligibleImmigrationStatus);
        //Assert.IsNull(personToCheck[1].EligibleImmigrationStatus);
        //Assert.IsNull(personToCheck[2].EligibleImmigrationStatus);
        //Assert.AreEqual("Non-Hispanic", personToCheck[0].Ethnicity);
        //Assert.AreEqual("Non-Hispanic", personToCheck[1].Ethnicity);
        //Assert.AreEqual("Non-Hispanic", personToCheck[2].Ethnicity);
        //Assert.AreEqual("White",        personToCheck[0].Race);
        //Assert.AreEqual("White",        personToCheck[1].Race);
        //Assert.AreEqual("White",        personToCheck[2].Race);
        Assert.AreEqual("LivingTogetherPartner20871724", personToCheck[0].PersonId);
        Assert.AreEqual("SELF29398552",                  personToCheck[1].PersonId);
        Assert.AreEqual("Daughter1089749324",            personToCheck[2].PersonId);
        Assert.AreEqual(true,                            personToCheck[0].EmploymentStatus);
        Assert.AreEqual(true,                            personToCheck[1].EmploymentStatus);
        Assert.AreEqual(false,                           personToCheck[2].EmploymentStatus);
        Assert.AreEqual("5624 Ellendale DR",             personToCheck[2].HomeAddress);
        Assert.AreEqual(true,                            personToCheck[2].PregnancyIndicator);
        Assert.AreEqual(false,                           personToCheck[0].AmericanIndianOrAlaskanNative);
        Assert.AreEqual(false,                           personToCheck[1].AmericanIndianOrAlaskanNative);
        Assert.AreEqual(true,                            personToCheck[2].AmericanIndianOrAlaskanNative);
    }

    [TestMethod]
    public void WorkXmlTest_PregnancyLoad()
    {
        var     data    = CreateDatabase();
        WorkXml workXml = null;
        try
        {
            workXml = new WorkXml(data, TestXmlPregnancy, receivedDate, false, false, "us-east-1", true);
        }
        catch (Exception ex)
        {
            Assert.Fail("Document Load threw exception: {0}", ex.Message);
        }

        Assert.IsNotNull(workXml);
        var personToCheck = workXml.App.ApplicationIndices.FirstOrDefault();
        Assert.IsNotNull(personToCheck);
        Assert.AreEqual(true,                      personToCheck.PregnancyIndicator);
        Assert.AreEqual(new DateTime(2014, 6, 27), personToCheck.PregnancyDueDate);
        Assert.AreEqual(false,                     personToCheck.FosterCareIndicator);
        Assert.AreEqual(false,                     personToCheck.DisabilityIndicator);
        Assert.AreEqual(true,                      personToCheck.CitizenIndicator);
        Assert.IsNull(personToCheck.RecentMedicalBillsIndicator);
    }

    [TestMethod]
    public void WorkXmlTest_PhoneLoad()
    {
        var     data    = CreateDatabase();
        WorkXml workXml = null;
        try
        {
            workXml = new WorkXml(data, TestXmlPhone, receivedDate, false, false, "us-east-1", true);
        }
        catch (Exception ex)
        {
            Assert.Fail("Document Load threw exception: {0}", ex.Message);
        }

        Assert.IsNotNull(workXml);
        Assert.IsNotNull(workXml.App);
        Assert.AreEqual(false, workXml.App.AnyoneHasHealthInsurance);
        var personToCheck = workXml.App.ApplicationIndices.FirstOrDefault();
        Assert.IsNotNull(personToCheck);
        Assert.AreEqual("9899649606", personToCheck.Phone);
        Assert.AreEqual("9899649607", personToCheck.OtherPhone);
        Assert.AreEqual("English",    personToCheck.PreferredSpokenLanguage);
        Assert.AreEqual("Female",     personToCheck.Sex);
        var testval = personToCheck.FulltimeStudent;
        Assert.AreEqual(false, personToCheck.FulltimeStudent);
        Assert.AreEqual(false, personToCheck.ParentOfUnder19Child);
        Assert.IsNull(personToCheck.PregnancyIndicator);
        Assert.AreEqual("Black / African American", personToCheck.Race);
        Assert.AreEqual(true,                       personToCheck.ReceivedItuservices);
        Assert.AreEqual(false,                      personToCheck.EligibleTribalBenefits);
        Assert.IsNull(personToCheck.HasNonEsicoverage);
        Assert.IsNull(personToCheck.HelpPayingMapremiums);
    }

    [TestMethod]
    public void WorkXmlTest_FlintLoad()
    {
        var     data    = CreateDatabase();
        WorkXml workXml = null;
        try
        {
            workXml = new WorkXml(data, TestXmlFlint, receivedDate, false, false, "us-east-1", true);
        }
        catch (Exception ex)
        {
            Assert.Fail("Document Load threw exception: {0}", ex.Message);
        }

        Assert.IsNotNull(workXml);
        Assert.IsNotNull(workXml.App);
        Assert.AreEqual(true, workXml.App.AnyoneHasHealthInsurance);
        var personToCheck = workXml.App.ApplicationIndices.FirstOrDefault();
        Assert.IsNotNull(personToCheck);
        Assert.IsNull(personToCheck.FulltimeStudent);
        Assert.AreEqual(true, personToCheck.FlintWaterVerification);
        Assert.IsNull(personToCheck.ParentOfUnder19Child);
        Assert.AreEqual(false,      personToCheck.PregnancyIndicator);
        Assert.AreEqual(true,       personToCheck.HasNonEsicoverage);
        Assert.AreEqual("Medicare", personToCheck.InsurancePolicySourceCode);
        Assert.AreEqual(true,       personToCheck.HelpPayingMapremiums);
        Assert.IsNotNull(personToCheck.DenialReasons);
        var denialReasons = personToCheck.DenialReasons.ToArray();
        Assert.IsNotNull("MAGI-PCR", denialReasons[0].ProgramCode);
        Assert.IsNotNull("IR3302",   denialReasons[0].ReasonCode);
        Assert.IsNotNull("MAGI-PCR", denialReasons[1].ProgramCode);
        Assert.IsNotNull("IR3419",   denialReasons[1].ReasonCode);
    }

    [TestMethod]
    public void WorkXmlTest_AddressLoad()
    {
        WorkXml workXml;
        var     data = CreateDatabase();
        try
        {
            workXml = new WorkXml(data, TestXmlAddress, receivedDate, false, false, "us-east-1", true);
        }
        catch (Exception ex)
        {
            Assert.Fail("Document Load threw exception: {0}", ex.Message);
            return;
        }

        Assert.IsNotNull(workXml);
        Assert.IsNotNull(workXml.App);
        Assert.AreEqual(false, workXml.App.AnyoneHasHealthInsurance);
        var personToCheck = workXml.App.ApplicationIndices.FirstOrDefault();
        Assert.IsNotNull(personToCheck);
        Assert.AreEqual("1309 s. michigan",        personToCheck.HomeAddress);
        Assert.AreEqual("saginaw",                 personToCheck.HomeCity);
        Assert.AreEqual("145",                     personToCheck.HomeCountyCode);
        Assert.AreEqual("Saginaw",                 personToCheck.HomeCounty);
        Assert.AreEqual("MI",                      personToCheck.HomeState);
        Assert.AreEqual("48602",                   personToCheck.HomeZip);
        Assert.AreEqual("123 Test Mailing Street", personToCheck.Address);
        Assert.AreEqual("Test City",               personToCheck.City);
        Assert.AreEqual("145",                     personToCheck.MailingCountyCode);
        Assert.AreEqual("Saginaw",                 personToCheck.MailingCounty);
        Assert.AreEqual("MI",                      personToCheck.State);
        Assert.AreEqual("48603",                   personToCheck.Zip);
        Assert.IsNull(personToCheck.HasNonEsicoverage);
    }

    [TestMethod]
    public void WorkXmlTest_ActivityDateWithTimeZone()
    {
        WorkXml workXml = null;
        var     data    = CreateDatabase();
        try
        {
            workXml = new WorkXml(data, TestXmlDateWithTimeZone, receivedDate, false, false, "us-east-1", true);
        }
        catch (Exception ex)
        {
            Assert.Fail("Document Load threw exception: {0}", ex.Message);
        }

        Assert.IsTrue(workXml.Valid, "WorkXml not valid. {0}", workXml.Exception);
        Assert.AreEqual(workXml.App.TransferDate, DateTime.Parse("2024-05-06T10:34:20.803"));
    }

    [TestMethod]
    public void WorkXmlTest_TestDocumentLoad()
    {
        WorkXml workXml;
        var     data = CreateDatabase();
        try
        {
            workXml = new WorkXml(data, TestXml, receivedDate, false, false, "us-east-1", true);
        }
        catch (Exception ex)
        {
            Assert.Fail("Document Load threw exception: {0}", ex.Message);
            return;
        }

        Assert.AreEqual("exch:AccountTransferRequest",           workXml.Document.SelectSingleNode("//ATXML").ChildNodes[0].Name);
        Assert.AreEqual("typ:assess-response",                   workXml.Document.SelectSingleNode("//DeterminationsReasons").ChildNodes[0].Name);
        Assert.AreEqual(true,                                    workXml.RedoStatus);
        Assert.AreEqual("MIPET180426T13511742",                  workXml.TransferID);
        Assert.AreEqual(receivedDate,                            workXml.App.ReceivedDate);
        Assert.AreEqual("N",                                     workXml.App.PendInvalidRelationships);
        Assert.AreEqual("N",                                     workXml.App.PendSameSex);
        Assert.AreEqual(string.Empty,                            workXml.App.OriginalFfmtransferId);
        Assert.AreEqual(workXml.TransferID,                      workXml.App.TransferId);
        Assert.AreEqual(DateTime.Parse("4/26/2018 11:51:17 am"), workXml.App.TransferDate);
        Assert.AreEqual("PE000163",                              workXml.App.ApplicationId);
        Assert.AreEqual("SOM-HUB",                               workXml.App.Source);
        //Assert.IsNotNull(workXml.App.ApplicationTransferXml);
        //Assert.IsNotNull(workXml.App.ApplicationTransferXml.Atxmlbin);
        Assert.AreEqual(1,                            workXml.App.ApplicationIndices.Count);
        Assert.AreEqual("Applicant",                  workXml.App.ApplicationIndices.First().PersonId);
        Assert.AreEqual(string.Empty,                 workXml.App.ApplicationIndices.First().ApplicantId);
        Assert.AreEqual(true,                         workXml.App.ApplicationIndices.First().Ssfsigner);
        Assert.AreEqual(true,                         workXml.App.ApplicationIndices.First().Hoh);
        Assert.AreEqual("18",                         workXml.App.ApplicationIndices.First().Relationship);
        Assert.AreEqual("Fayfay",                     workXml.App.ApplicationIndices.First().LastName);
        Assert.AreEqual("Faybo",                      workXml.App.ApplicationIndices.First().FirstName);
        Assert.AreEqual("Rash",                       workXml.App.ApplicationIndices.First().MiddleName);
        Assert.AreEqual("183135128",                  workXml.App.ApplicationIndices.First().Ssn);
        Assert.AreEqual(DateTime.Parse("1971-05-27"), workXml.App.ApplicationIndices.First().Dob);
        Assert.AreEqual("6190 Mountainside CR",       workXml.App.ApplicationIndices.First().Address);
        Assert.AreEqual("PORT HURON",                 workXml.App.ApplicationIndices.First().City);
        Assert.AreEqual("MI",                         workXml.App.ApplicationIndices.First().State);
        Assert.AreEqual("NONE",                       workXml.App.ApplicationIndices.First().MagimichildDetermination);
        Assert.AreEqual("NONE",                       workXml.App.ApplicationIndices.First().Magimcdetermination);
        Assert.AreEqual("",                           workXml.App.ApplicationIndices.First().Pend);
        //Assert.AreEqual("SELF29398552", workXml.App.Indexes[1].PersonID);
        //Assert.AreEqual("Applicant29398552", workXml.App.Indexes[1].ApplicantID);
        //Assert.AreEqual(true, workXml.App.Indexes[1].IsSSFSigner);
        //Assert.AreEqual(true, workXml.App.Indexes[1].IsHOH);
        //Assert.AreEqual("53", workXml.App.Indexes[1].Relationship);
        //Assert.AreEqual("Yurgel", workXml.App.Indexes[1].LastName);
        //Assert.AreEqual("Jessica", workXml.App.Indexes[1].FirstName);
        //Assert.AreEqual("R", workXml.App.Indexes[1].MiddleName);
        //Assert.AreEqual("595545665", workXml.App.Indexes[1].SSN);
        //Assert.AreEqual(new SmartDate("1986-04-30").ToString(), workXml.App.Indexes[1].DOB);
        //Assert.AreEqual("5624  Ellendale DR", workXml.App.Indexes[1].Address);
        //Assert.AreEqual("Lansing", workXml.App.Indexes[1].City);
        //Assert.AreEqual("MI", workXml.App.Indexes[1].State);
        //Assert.AreEqual("DENIED", workXml.App.Indexes[1].MAGIMIChildDetermination);
        //Assert.AreEqual("APPROVED", workXml.App.Indexes[1].MAGIMCDetermination);
        //Assert.AreEqual("Y", workXml.App.Indexes[1].Pend);
        //Assert.AreEqual("Daughter1089749324", workXml.App.Indexes[2].PersonID);
        //Assert.AreEqual("Applicant1089749324", workXml.App.Indexes[2].ApplicantID);
        //Assert.AreEqual(false, workXml.App.Indexes[2].IsSSFSigner);
        //Assert.AreEqual(false, workXml.App.Indexes[2].IsHOH);
        //Assert.AreEqual("19", workXml.App.Indexes[2].Relationship);
        //Assert.AreEqual("Kruger", workXml.App.Indexes[2].LastName);
        //Assert.AreEqual("Penelope", workXml.App.Indexes[2].FirstName);
        //Assert.AreEqual("Aeris", workXml.App.Indexes[2].MiddleName);
        //Assert.AreEqual("184939137", workXml.App.Indexes[2].SSN);
        //Assert.AreEqual(new SmartDate("2012-08-26").ToString(), workXml.App.Indexes[2].DOB);
        //Assert.AreEqual("5624  Ellendale DR", workXml.App.Indexes[2].Address);
        //Assert.AreEqual("Lansing", workXml.App.Indexes[2].City);
        //Assert.AreEqual("MI", workXml.App.Indexes[2].State);
        //Assert.AreEqual("DENIED", workXml.App.Indexes[2].MAGIMIChildDetermination);
        //Assert.AreEqual("DENIED", workXml.App.Indexes[2].MAGIMCDetermination);
        //Assert.AreEqual("N", workXml.App.Indexes[2].Pend);
        Assert.AreEqual("",                       workXml.App.Pend);
        Assert.AreEqual(1,                        workXml.App.Transitions.Count);
        Assert.AreEqual("RECEIVED",               workXml.App.Transitions.First().StatusCode);
        Assert.AreEqual("Added by DEG Processor", workXml.App.Transitions.First().Comments);
    }
}