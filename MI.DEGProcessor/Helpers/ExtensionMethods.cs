using System.Reflection;
using MI.Common.Extensions;

namespace MI.DEGProcessor.Helpers;

public static class ExtensionMethods
{
    public static void CopyPropertiesTo<T>(this T source, object dest)
    {
        var plist = from prop in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    where prop.CanRead
                    select prop;

        var destProps = (from prop in dest.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         where prop.CanWrite
                         select prop).ToList();

        foreach (var prop in plist)
        {
            var destProp = destProps.FirstOrDefault(p => p.Name.Equals(prop.Name, StringComparison.Ordinal));
            if (destProp != null && destProp.CanWrite)
            {
                if (prop.IsNonStringEnumerable())
                {
                    //foreach (var o in prop.GetValue(source, null))
                    //{
                    //}
                }
                else
                {
                    destProp.SetValue(dest, prop.GetValue(source, null), null);
                }
            }
        }
    }

    public static void CopyIndexPropertiesTo<T>(this T source, object dest)
    {
        var indexFields =
            "[HomeAddress],[HomeCity],[HomeState],[HomeZip],[HomeCountyCode],[HomeCounty],[MailingCountyCode],[MailingCounty],[Email],[Phone]";
        indexFields +=
            ",[OtherPhone],[PreferredSpokenLanguage],[Sex],[FulltimeStudent],[FlintWaterVerification],[ParentOfUnder19Child],[PregnancyIndicator]";
        indexFields +=
            ",[PregnancyDueDate],[NeedHealthCoverage],[FosterCareIndicator],[DisabilityIndicator],[CitizenIndicator],[RecentMedicalBillsIndicator]";
        indexFields +=
            ",[EligibleImmigrationStatus],[Ethnicity],[Race],[EmploymentStatus],[EmploymentStatusCode],[AmericanIndianOrAlaskanNative]";
        indexFields +=
            ",[ReceivedITUServices],[EligibleTribalBenefits],[HasNonESICoverage],[InsurancePolicySourceCode],[HelpPayingMAPremiums],[ESIEnrolledIndicator]";

        var plist = from prop in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    where prop.CanRead
                    select prop;

        var destProps = (from prop in dest.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         where prop.CanWrite
                         select prop).ToList();

        foreach (var prop in plist)
        {
            var destProp = destProps.FirstOrDefault(p => p.Name.Equals(prop.Name, StringComparison.Ordinal));
            if (destProp != null && destProp.CanWrite && indexFields.IndexOf("[" + destProp.Name + "]") >= 0)
            {
                if (prop.IsNonStringEnumerable())
                {
                    //foreach (var o in prop.GetValue(source, null))
                    //{
                    //}
                }
                else
                {
                    destProp.SetValue(dest, prop.GetValue(source, null), null);
                }
            }
        }
    }
}