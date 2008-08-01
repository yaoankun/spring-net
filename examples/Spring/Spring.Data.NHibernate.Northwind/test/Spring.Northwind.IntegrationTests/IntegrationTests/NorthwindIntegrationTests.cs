#region License

/*
 * Copyright � 2002-2007 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

#region Imports

using System;
using System.Collections;
using System.Data;
using NHibernate;
using NUnit.Framework;
using Spring.Data.NHibernate;
using Spring.Northwind.Dao;
using Spring.Northwind.Domain;

#endregion

namespace Spring.Northwind.IntegrationTests
{
    /// <summary>
    /// This class contains tests for 
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <version>$Id:$</version>
    [TestFixture]
    public class NorthwindIntegrationTests : AbstractDaoIntegrationTests
    {
        private ICustomerDao customerDao;
        private IOrderDao orderDao;

        private ISessionFactory sessionFactory;

        public ICustomerDao CustomerDao
        {
            set { customerDao = value; }
        }


        public IOrderDao OrderDao
        {
            set { orderDao = value; }
        }


        public ISessionFactory SessionFactory
        {
            set { sessionFactory = value; }
        }

        [Test]
        public void SanityCheck()
        {

            Assert.IsNotNull(customerDao, "customer dao is null");

        }

        [Test]
        public void CustomerDaoTests()
        {
            Assert.AreEqual(91, customerDao.FindAll().Count);

            Customer c = new Customer();
            c.Id = "MPOLL";           
            c.CompanyName = "Interface21";
            customerDao.Save(c);
            c = customerDao.FindById("MPOLL");
            Assert.AreEqual(c.Id, "MPOLL");
            Assert.AreEqual(c.CompanyName, "Interface21");

            //Without flushing, nothing changes in the database:
            int customerCount = (int)AdoTemplate.ExecuteScalar(CommandType.Text, "select count(*) from Customers");           
            Assert.AreEqual(91, customerCount);

            //Flush the session to execute sql in the db.
            SessionFactoryUtils.GetSession(sessionFactory, true).Flush();
           
            //Now changes are visible outside the session but within the same database transaction
            customerCount = (int)AdoTemplate.ExecuteScalar(CommandType.Text, "select count(*) from Customers");
            Assert.AreEqual(92, customerCount);
            
            Assert.AreEqual(92, customerDao.FindAll().Count);

            c.CompanyName = "SpringSource";

            customerDao.SaveOrUpdate(c);

            c = customerDao.FindById("MPOLL");
            Assert.AreEqual(c.Id, "MPOLL");
            Assert.AreEqual(c.CompanyName, "SpringSource");

            customerDao.Delete(c);


            SessionFactoryUtils.GetSession(sessionFactory, true).Flush();
            customerCount = (int)AdoTemplate.ExecuteScalar(CommandType.Text, "select count(*) from Customers");
            Assert.AreEqual(92, customerCount);

            try
            {
                c = customerDao.FindById("MPOLL");
                Assert.Fail("Should have thrown HibernateObjectRetrievalFailureException when finding customer with Id = MPOLL");
            }
            catch (HibernateObjectRetrievalFailureException e)
            {
                Assert.AreEqual("Customer", e.PersistentClassName);
            }

        }

        [Test]
        public void ProductDaoTests()
        {
            Assert.AreEqual(830, orderDao.FindAll().Count);

            Order order = new Order();
            Customer customer = customerDao.FindById("PICCO");
            order.Customer = customer;
            order.ShipCity = "New York";
            
            orderDao.Save(order);
            int orderId = order.Id;
            order = orderDao.FindById(orderId);
            Assert.AreEqual("PICCO", order.Customer.Id);
            Assert.AreEqual("New York", order.ShipCity);

            //SessionFactoryUtils.GetSession(sessionFactory, true).Flush();
            // Required trip to db to get idendity column, so data is visible

            int ordersCount = (int)AdoTemplate.ExecuteScalar(CommandType.Text, "select count(*) from Orders");
            Assert.AreEqual(831, ordersCount);

            Assert.AreEqual(831, orderDao.FindAll().Count);

            order.ShipCity = "Sao Paulo";
            orderDao.SaveOrUpdate(order);

            order = orderDao.FindById(orderId);
            Assert.AreEqual("PICCO", order.Customer.Id);
            Assert.AreEqual("Sao Paulo", order.ShipCity);


            orderDao.Delete(order);

            //Without flushing, nothing changes in the database:
            SessionFactoryUtils.GetSession(sessionFactory, true).Flush();

            ordersCount = (int)AdoTemplate.ExecuteScalar(CommandType.Text, "select count(*) from Orders");
            Assert.AreEqual(830, ordersCount);

            try
            {
                order = orderDao.FindById(orderId);
                Assert.Fail("Should have thrown HibernateObjectRetrievalFailureException when finding order with Id = " + orderId);
            }
            catch (HibernateObjectRetrievalFailureException e)
            {
                Assert.AreEqual("Order", e.PersistentClassName);
            }

        }
    }
}