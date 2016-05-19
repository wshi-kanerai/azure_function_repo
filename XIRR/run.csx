using System;
using System.Net;
public class UserError : Exception
{
    public UserError(string message) : base(message) { }
}

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");

    // Get request body
    dynamic data = await req.Content.ReadAsAsync<object>();

    // Set name to query string or body data
    double[] cashflow = data?.cashflow;
    string[] dates = data?.dates;

    try
    {
        double xirr = XIRR(cashflow, dates);
        return req.CreateResponse(HttpStatusCode.OK, xirr.ToString());
        //Console.WriteLine(xirr.ToString());
    }
    catch (UserError e)
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, e.Message);
        //Console.WriteLine(e.Message);
    }
    catch (Exception e)
    {
        return req.CreateResponse(HttpStatusCode.InternalServerError, "Internal Error: " + e.Message);
        //Console.WriteLine("Internal Error:" + e.GetType().ToString() + " " + e.Message);
    }
}

public static DateTime parseDatetime(string date)
{
    try
    {
        return DateTime.Parse(date);
    }
    catch
    {
        throw new UserError("Unrecognized date: " + date);
    }
}

public static double XIRR(double[] cashflow, string[] dates)
{
    if (cashflow.Length != dates.Length)
    {
        throw new UserError("Length of cashflow != Length of dates");
    }
    if (cashflow.Length < 2)
    {
        throw new UserError("Cashflow must be more than one period");
    }
    int N = cashflow.Length;
    DateTime t0 = parseDatetime(dates[0]);
    double[] dt = new double[N];
    for (int i = 0; i < N; i++)
    {
        DateTime t = parseDatetime(dates[i]);
        dt[i] = (t - t0).Days / 365.0;
    }

    double x = 0;
    double x1 = 0;
    double err = 1e+100;
    int iter = 0;
    const int MAXITER = 20;
    while (err > 0.001 && iter < MAXITER)
    {
        double f = 0;
        double df = 0;
        for (int i = 0; i < N; i++)
        {
            f += cashflow[i] * Math.Pow((1.0 + x), -dt[i]);
            df += -cashflow[i] * Math.Pow((1.0 + x), -dt[i] - 1) * dt[i];
        }
        x1 = x - f / df;
        err = Math.Abs(x1 - x);
        x = x1;
        iter++;
    }
    if (iter == MAXITER)
    {
        throw new UserError("Cannot find solution");
    }
    return x;
}

