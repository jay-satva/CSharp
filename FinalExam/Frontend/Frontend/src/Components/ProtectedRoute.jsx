import React from "react";
import { Navigate } from "react-router-dom";
import { getAccessToken } from "../auth/authStorage";

const ProtectedRoute = ({ children }) => {
  return getAccessToken() ? children : <Navigate to="/login" replace />;
};

export default ProtectedRoute;
