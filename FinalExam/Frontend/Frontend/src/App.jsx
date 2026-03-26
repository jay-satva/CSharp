import React from "react";
import { BrowserRouter as Router, Routes, Route, Navigate } from "react-router-dom";
import Login from "./Components/Login";
import Signup from "./Components/Signup";
import Dashboard from "./Components/Dashboard";
import ConnectionPage from "./Components/ConnectionPage";
import Account from "./Components/Forms/Account";
import Customer from "./Components/Forms/Customer";
import Item from "./Components/Forms/Item";
import Invoice from "./Components/Forms/Invoice";
import InvoiceListing from "./Components/InvoiceListing";
import AuthCallback from "./Components/AuthCallback";
import ProtectedRoute from "./Components/ProtectedRoute";
import { getAccessToken } from "./auth/authStorage";
import "bootstrap/dist/css/bootstrap.min.css";

function App() {
  return (
    <Router>
      <div className="App">
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/signup" element={<Signup />} />
          <Route path="/auth/callback" element={<AuthCallback />} />
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <Dashboard />
              </ProtectedRoute>
            }
          />
          <Route
            path="/account"
            element={
              <ProtectedRoute>
                <Account />
              </ProtectedRoute>
            }
          />
          <Route
            path="/customer"
            element={
              <ProtectedRoute>
                <Customer />
              </ProtectedRoute>
            }
          />
          <Route
            path="/item"
            element={
              <ProtectedRoute>
                <Item />
              </ProtectedRoute>
            }
          />
          <Route
            path="/invoices"
            element={
              <ProtectedRoute>
                <InvoiceListing />
              </ProtectedRoute>
            }
          />
          <Route
            path="/invoice/new"
            element={
              <ProtectedRoute>
                <Invoice />
              </ProtectedRoute>
            }
          />
          <Route
            path="/invoice/edit/:id"
            element={
              <ProtectedRoute>
                <Invoice />
              </ProtectedRoute>
            }
          />
          <Route
            path="/connection"
            element={
              <ProtectedRoute>
                <ConnectionPage />
              </ProtectedRoute>
            }
          />
          <Route path="/" element={<Navigate to={getAccessToken() ? "/dashboard" : "/login"} />} />
        </Routes>
      </div>
    </Router>
  );
}

export default App;
