using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// You will use the SQL server provider; however, it would also 
// be permissible to use the ADO.NET factory pattern for greater flexibility.
using System.Data;
using System.Data.SqlClient;
using AutoLotDAL.Models;

namespace AutoLotDAL.DataOperations
{
    public class InventoryDAL
    {
        // Fields
        private readonly string _connectionString;

        // Add a class-level variable to hold a 
        // connection that will be used by the data access code.
        private SqlConnection _sqlConnection = null;

        #region Adding the Constructors

        // Create a constructor that takes a string parameter 
        // (connection string) and assigns the value to a class-level variable.
        public InventoryDAL() : this(@"Data Source = (localdb)\mssqllocaldb;Integrated
        Security=true;Initial Catalog=AutoLot")
        {
        }

        // Next, create a default constructor that passes a default connection
        // string to the other constructor. (This enables the calling code to
        // change the connection string if necessary from the default.
        public InventoryDAL(string connectionString)
            => _connectionString = connectionString;
        #endregion

        #region Methods for Opening and Closing the Connection
        // Add a method to open the connection (OpenConnection())
        private void OpenConnection()
        {
            _sqlConnection = new SqlConnection { ConnectionString = _connectionString };
            _sqlConnection.Open();
        }

        // Add a method to close the connection (CloseConnection()).
        // This method will check the state of the connection, and if it's
        // not closed, it will call Close() on the connection
        private void CloseConnection()
        {
            if (_sqlConnection?.State != ConnectionState.Closed)
            {
                _sqlConnection?.Close();
            }
        }
        #endregion

        #region Adding the Selection Methods 
        // Selection method for getting all the records from the inventory table
        public List<Car> GetAllInventory()
        {
            OpenConnection();
            // This will hold the records.
            List<Car> inventory = new List<Car>();

            // Prep command object.
            string sql = "Select * From Inventory";
            using (SqlCommand command = new SqlCommand(sql, _sqlConnection))
            {
                command.CommandType = CommandType.Text;
                SqlDataReader dataReader = command.ExecuteReader(CommandBehavior.CloseConnection);
                while (dataReader.Read())
                {
                    inventory.Add(new Car
                    {
                        CarId = (int)dataReader["CarId"],
                        Color = (string)dataReader["Color"],
                        Make = (string)dataReader["Make"],
                        PetName = (string)dataReader["PetName"]
                    });
                }
                dataReader.Close();
            }
            return inventory;
        }

        // Selection method for getting a single car based on the CarId
        public Car GetCar(int id)
        {
            OpenConnection();
            Car car = null;
            string sql = $"Select * From Inventory where CarId = {id}";
            using (SqlCommand command = new SqlCommand(sql, _sqlConnection))
            {
                command.CommandType = CommandType.Text;
                SqlDataReader dataReader = command.ExecuteReader(CommandBehavior.CloseConnection);
                while (dataReader.Read())
                {
                    car = new Car
                    {
                        CarId = (int)dataReader["CarId"],
                        Color = (string)dataReader["Color"],
                        Make = (string)dataReader["Make"],
                        PetName = (string)dataReader["PetName"]
                    };
                }
                dataReader.Close();
            }
            return car;
        }
        #endregion

        #region Inserting a new Car
        // Add a public method that takes four parameters that map to the four
        // columns of the InventoryTable(CarId, Color, Make, and Petname). You 
        // use these arguments to format a string type to insert the new record.
        // Finally, you use your SqlConnection object to execute the SQL statement.
        public void InsertAuto(string color, string make, string petName)
        {
            OpenConnection();
            // Format and execute SQL statement.
            string sql = $"Insert Into Inventory (Make, Color, PetName)" +
                $" Values ('{make}', '{color}', '{petName}')";
            // Execute using our connection.
            using (SqlCommand command = new SqlCommand(sql, _sqlConnection))
            {
                command.CommandType = CommandType.Text;
                command.ExecuteNonQuery();
            }
            CloseConnection();
        }

        // Previous method takes three string values and works as long as
        // the calling code passes the strings in the correct order. A better
        // method uses the Car model to make a strongly typed method, ensuring
        // all the properties are passed into the method in the correct order.

        // Add another InsertAuto() method that takes a 
        // Car model as a parameter to your InventoryDAL class
        //public void InsertAuto(Car car)
        //{
        //    OpenConnection();
        //    // format and execute SQL statement
        //    string sql = "Insert Into Inventory (Make, Color, PetName) Values " +
        //        $"('{car.Make}', '{car.Color}', '{car.PetName}')";

        //    // Execute using our connection.
        //    using (SqlCommand command = new SqlCommand(sql, _sqlConnection))
        //    {
        //        command.CommandType = CommandType.Text;
        //        command.ExecuteNonQuery();
        //    }
        //    CloseConnection();
        //}

        // Re-worked version of the InsertAuto() 
        // method to leverage parameter objects
        public void InsertAuto(Car car)
        {
            OpenConnection();
            // Note the "placeholders" in the SQL query.
            string sql = "Insert Into Inventory" +
                "(Make, Color, PetName) Values" +
                "(@Make, @Color, @Petname)";

            // This command will have internal parameters
            using (SqlCommand command = new SqlCommand(sql, _sqlConnection))
            {
                // Fill params collection.
                SqlParameter parameter = new SqlParameter
                {
                    ParameterName = "@Make",
                    Value = car.Make,
                    SqlDbType = SqlDbType.Char,
                    Size = 10
                };
                command.Parameters.Add(parameter);

                parameter = new SqlParameter
                {
                    ParameterName = "@Color",
                    Value = car.Color,
                    SqlDbType = SqlDbType.Char,
                    Size = 10
                };
                command.Parameters.Add(parameter);

                parameter = new SqlParameter
                {
                    ParameterName = "@PetName",
                    Value = car.PetName,
                    SqlDbType = SqlDbType.Char,
                    Size = 10
                };
                command.Parameters.Add(parameter);

                command.ExecuteNonQuery();
                CloseConnection();
            }
        }
        #endregion

        #region Adding Deletion Logic
        // Add a method for deleting records. Inside it, code a try/catch
        // block that handles the possibility of attempting to delete a car
        // that is currently on order for an individual in the Customers table.
        public void DeleteCar(int id)
        {
            OpenConnection();
            // Get ID of car to delete, then do so.
            string sql = $"Delete from Inventory where CarId = '{id}'";
            using (SqlCommand command = new SqlCommand(sql, _sqlConnection))
            {
                try
                {
                    command.CommandType = CommandType.Text;
                    command.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception("Sorry! That car is on order!", ex);
                    throw error;
                }
            }
            CloseConnection();
        }
        #endregion

        #region Adding Update Logic
        // Ideally, you want to have a set of methods that allow the caller
        // to update a record in a variety of ways. However, for this simple
        // data access library, you will define a single method that allows
        // the caller to update the pet name of a given automobile
        public void UpdateCarPetName(int id, string newPetName)
        {
            OpenConnection();
            // Get ID of car to modify the pet name.
            string sql = $"Update Inventory Set PetName = " +
                $"'{newPetName}' Where CarId = '{id}'";
            using (SqlCommand command = new SqlCommand(sql, _sqlConnection))
            {
                command.ExecuteNonQuery();
            }
            CloseConnection();
        }
        #endregion


    }
}
