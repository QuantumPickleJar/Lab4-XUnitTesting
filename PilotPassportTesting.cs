using System.Collections.ObjectModel;
using Lab4;
using Lab4.Interfaces;
using Xunit.Sdk;

namespace TestingTesting123;

/// <summary>
/// Fixture: this class establishes a static connection 
/// context amongst all tests in PilotPassportTesting.cs.
/// 
/// It also defines an airport with test data to reduce 
/// the amount of code that needs to be entered since 
/// we're working with CRUD operations in these tests.
/// </summary>
public class CockroachFixture : IDisposable
{
    public IBusinessLogic brains { get; private set; }

    /// <summary>
    /// Test airport to minimize manually entered data
    /// </summary>
    public Airport TestAirport
    {
        get { return _testAirport; }
    }
    private Airport _testAirport;

    /// <summary>
    /// Constructs a new CockroachFixture to help provide 
    /// data to the testing class
    /// </summary>
    public CockroachFixture()
    {
        _testAirport = new Airport
        {
            Id = "TEST",
            City = "Notrealsville",
            DateVisited = DateTime.UnixEpoch,
            Rating = 4
        };
        brains = new BusinessLogic();
        // just in case, delete the test airport
        RemoveTestAirportIfPresent();
    }

    /// <summary>
    /// Helper method to reduce line usage: checks for the
    /// test airport from the TestFixture.  If found, it 
    /// removes it.  Used mainly for preconditioning our 
    /// update and add operations.
    /// </summary>
    private void RemoveTestAirportIfPresent()
    {
        if (brains.FindAirport(TestAirport.Id) != null)
        {
            brains.DeleteAirport(TestAirport);
        }
    }

    /// <summary>
    /// release managed resources: since BusinessLogic
    /// doesn't implement IDisposable, we're just going
    /// to set it to null here AFTER we purge leftovers
    /// from failed tests
    /// </summary>
    public void Dispose()
    {
        try
        {
            RemoveTestAirportIfPresent();
        }
        // ignore the exception if TestAirport deleted as desired
        finally
        {
            brains = null;
        }
    }
}

public class PilotPassportTesting : IClassFixture<CockroachFixture>
{
    //private static IBusinessLogic brains = new BusinessLogic();
    private readonly CockroachFixture fixture;

    public PilotPassportTesting(CockroachFixture fixture)
    {
        this.fixture = new CockroachFixture();
    }

    /// <summary>
    /// Helper method to make sure the test method is in the database on the fly
    /// </summary>
    private void VerifyTestAirportPresence()
    {
        if (fixture.brains.FindAirport(fixture.TestAirport.Id) == null)
            AddTestAirport();
    }


    /// <summary>
    /// Space-saving helper method to reduce code needed to add the test airport
    /// to the database for testing
    /// </summary>
    private void AddTestAirport()
    {
        fixture.brains.AddAirport(
            fixture.TestAirport.Id,
            fixture.TestAirport.City,
            fixture.TestAirport.DateVisited,
            fixture.TestAirport.Rating);
    }

    /* ==================== [ BASE CASES ] ====================== */

    /// <summary>
    /// Verifies that the GetAirports() function works, and more importantly 
    /// that it can reach the database.
    /// </summary>
    [Fact]
    public void TestGetAirports()
    {
        try
        {
            var airports = fixture.brains.GetAirports();
            Assert.NotEmpty(airports);
        }
        catch (Exception e)
        {
            Assert.Fail(e.ToString());
        }
    }


    /// <summary>
    /// Tests that the add functionality is able to make the changes to the
    /// database so that the count is updated when it comes back
    /// </summary>
    [Fact]
    public void TestAddAirport()
    {
        int expected, actual;
        expected = fixture.brains.GetAirports().Count + 1;

        // Add the test airport if it doesn't already exist
        VerifyTestAirportPresence();

        actual = fixture.brains.GetAirports().Count;

        Assert.Equal(expected, actual);
    }


    /// <summary>
    /// Tests that the update operation is able to modify a
    /// </summary>
    [Fact]
    public void TestUpdateAirport()
    {
        // Ensure the target is present for updating
        VerifyTestAirportPresence();

        Airport expected = new Airport
        {
            Id = fixture.TestAirport.Id,
            City = "North Antsouthlantica",
            DateVisited = fixture.TestAirport.DateVisited,
            Rating = fixture.TestAirport.Rating
        };

        fixture.brains.EditAirport(
            expected.Id,    // matches TestAirport's Id
            expected.City,
            expected.DateVisited,
            expected.Rating);

        // the new city should be accessible by the same ID
        Airport? actual = fixture.brains.FindAirport(expected.Id);

        if (actual != null)
            Assert.Equal(expected.City, actual.City);
    }

    /// <summary>
    /// Tests that the Remove operation is in working order
    /// </summary>
    [Fact]
    public void TestRemoveAirport()
    {
        // Ensure the target is present for updating
        VerifyTestAirportPresence();

        // Take the count *after* the verification since it's being removed
        int expected = fixture.brains.GetAirports().Count - 1;
        int actual;


        // remove the updated airport (will have the Test ID)
        var deleted = fixture.brains.DeleteAirport(fixture.TestAirport);
        Assert.DoesNotContain(deleted, fixture.brains.GetAirports());

        // assert that the Count is one less
        actual = fixture.brains.GetAirports().Count;
        Assert.Equal(expected, actual);
    }

    /* ==================== [ EDGE CASES ] ==================== */

    /// <summary>
    /// Tests that the Add operation is not successful with any null fields 
    /// present in the request.  Doubly-intensive check in that we both check
    /// the appropriate Exception type, and that no changes were made regardless
    /// </summary>
    [Fact]
    public void TestAddAirport_RejectsNulls()
    {
        int expected, actual;
        // no changes should be made, so ensure the count remains unchanged
        expected = fixture.brains.GetAirports().Count;

        // The BL should throw an ArgumentNullException upon null City
        Assert.Throws<ArgumentNullException>(() =>
        {
            fixture.brains.AddAirport(
                fixture.TestAirport.Id,
                null,
                fixture.TestAirport.DateVisited,
                fixture.TestAirport.Rating);
        });

        // same goes for null Id
        Assert.Throws<ArgumentNullException>(() =>
        {
            fixture.brains.AddAirport(
                null,
                fixture.TestAirport.City,
                fixture.TestAirport.DateVisited,
                fixture.TestAirport.Rating);
        });

        // as beautiful as that short lump of code above was, (and some could argue
        // that the above code sufficiently covers the scope...) but in the interest
        // of the assignment, we'll still make sure no changes were made. 

        actual = fixture.brains.GetAirports().Count;
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Tests that the program rejects illegal dates (dates that have not yet
    /// occurred) and that the appropriate exception is thrown when adding an 
    /// Airport.
    /// </summary>
    [Fact]
    public void TestAddAirport_RejectsIllegalDate()
    {
        int actual;
        int expected = fixture.brains.GetAirports().Count;

        // BL should throw an ArgOutOfRange exception if user adds a future date
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            fixture.brains.AddAirport(
                fixture.TestAirport.Id,
                fixture.TestAirport.City,
                DateTime.Now.AddDays(1),    // add tomorrow's date
                fixture.TestAirport.Rating);
        });


        actual = fixture.brains.GetAirports().Count;
        Assert.Equal(expected, actual);

        // assert that our bad ID message will go where it's needed
        Assert.Contains(AirportErrorHandling.Messages.InvalidDateMsg,
            exception.Message);
    }

    /// <summary>
    /// Tests that repeatedly adding an airport with the same information is
    /// rejected by the custom AirportException.
    /// </summary>
    [Fact]
    public void TestAddAirport_RejectsDuplicate()
    {
        int expected = fixture.brains.GetAirports().Count + 1;

        // make sure the original Airport is already present in the database
        VerifyTestAirportPresence();

        // ensure custom exception type is thrown upon re-adding TestAirport
        var exception = 
            Assert.Throws<AirportErrorHandling.AirportException>(AddTestAirport);

        int actual = fixture.brains.GetAirports().Count;
        Assert.Equal(expected, actual);

        // assert our bad ID message will go where it's needed
        Assert.Contains(AirportErrorHandling.Messages.DuplicateIdMsg,
            exception.Message);
    }


    /// <summary>
    /// Tests that the appropriate custom exception is thrown
    /// when a). an Airport with an Id that is too long, and
    /// b). an Airport with an Id that is too short.
    /// Also verifies that the collection remains unchanged
    /// </summary>
    [Fact]
    public void TestAddAirport_RejectsOutOfRangeId()
    {
        var expected = fixture.brains.GetAirports();
        const string longId = "LONGID";
        const string shortId = "ID";

        // test that a 3-character Id is rejected 
        var shortIdEx = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            fixture.brains.AddAirport(
                shortId,
                fixture.TestAirport.City,
                fixture.TestAirport.DateVisited,
                fixture.TestAirport.Rating);
        });

        // test that a 5-character Id is rejected
        var longIdEx = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            fixture.brains.AddAirport(
                longId,
                fixture.TestAirport.City,
                fixture.TestAirport.DateVisited,
                fixture.TestAirport.Rating);
        });


        // ensure that the collection is unchanged
        Assert.StrictEqual(expected, fixture.brains.GetAirports());

        // assert that our bad Id messages will go where it's needed
        Assert.Contains(AirportErrorHandling.Messages.IllegalIdMsg,
            shortIdEx.Message);
        Assert.Contains(AirportErrorHandling.Messages.IllegalIdMsg,
            longIdEx.Message);
    }

    /// <summary>
    /// Tests that the program rejects illegal dates (dates that have not yet
    /// occurred) and that the appropriate exception is thrown when updating 
    /// an Airport.
    /// </summary>
    [Fact]
    public void TestUpdateAirport_RejectsIllegalDate()
    {
        // Ensure the target is present for updating
        VerifyTestAirportPresence();

        int expected, actual;
        expected = fixture.brains.GetAirports().Count;

        // BL should throw an ArgOutOfRange exception if user adds a future date
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            fixture.brains.EditAirport(
                fixture.TestAirport.Id,
                fixture.TestAirport.City,
                DateTime.Now.AddDays(1),    // add tomorrow's date
                fixture.TestAirport.Rating);
        });

        actual = fixture.brains.GetAirports().Count;
        Assert.Equal(expected, actual);

        // assert that our bad ID message will go where it's needed
        Assert.Contains(AirportErrorHandling.Messages.InvalidDateMsg,
            exception.Message);
    }

    /// <summary>
    /// Test updates fields that allow null values to be stored to 
    /// ensure they are rejected before hitting the database.
    /// DateTime and int32s don't allow being set to null, so 
    /// we only test Id and City.
    /// </summary>
    /// 
    [Fact]
    public void TestUpdateAirport_RejectsNulls()
    {
        // store the state of the database before we do any tests
        var expected = fixture.brains.GetAirports();

        // Ensure the target is present for updating
        VerifyTestAirportPresence();

        // The BL should throw an ArgumentNullException upon null City
        Assert.Throws<ArgumentNullException>(() =>
        {
            fixture.brains.EditAirport(
                fixture.TestAirport.Id,
                null,
                fixture.TestAirport.DateVisited,
                fixture.TestAirport.Rating);
        });

        // same goes for null Id
        Assert.Throws<ArgumentNullException>(() =>
        {
            fixture.brains.EditAirport(
                null,
                fixture.TestAirport.City,
                fixture.TestAirport.DateVisited,
                fixture.TestAirport.Rating);
        });

        // ensure that the collection is unchanged
        Assert.StrictEqual(expected, fixture.brains.GetAirports());
    }

    /// <summary>
    /// Tests that Updates with illegal ratings (where 1 <= x <= 5) are 
    /// rejected.  Moreover, we also check that the appropriate custom 
    /// exception message is being supplied.
    /// </summary>
    [Fact]
    public void TestUpdateAirport_RejectsOutOfRangeRating()
    {
        // Ensure the target is present for updating
        VerifyTestAirportPresence();

        const int leftBoundRating = -1;
        const int rightBoundRating = 6;

        int expected = fixture.TestAirport.Rating;

        // test rejecting values < 0
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            fixture.brains.EditAirport(
                fixture.TestAirport.Id,
                fixture.TestAirport.City,
                fixture.TestAirport.DateVisited,
                leftBoundRating);
        });


        // test rejecting of values > 5
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            fixture.brains.EditAirport(
                fixture.TestAirport.Id,
                fixture.TestAirport.City,
                fixture.TestAirport.DateVisited,
                rightBoundRating);
        });

        // verify that no changes were made
        var airport = fixture.brains.FindAirport(fixture.TestAirport.Id);
        Assert.Equal(expected, airport.Rating);
    }
}