import React, { useEffect } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { setAuth } from "../auth/authStorage";

const AuthCallback = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  useEffect(() => {
    const accessToken = searchParams.get("accessToken");
    const userId = searchParams.get("userId");
    const name = searchParams.get("name");
    const email = searchParams.get("email");
    const intuitSub = searchParams.get("intuitSub");
    const status = searchParams.get("status");

    if (!accessToken || !userId) {
      navigate("/login", { replace: true });
      return;
    }

    setAuth({
      accessToken,
      userId,
      name,
      email,
      intuitSub,
    });

    const destination = status ? `/dashboard?status=${encodeURIComponent(status)}` : "/dashboard";
    navigate(destination, { replace: true });
  }, [navigate, searchParams]);

  return (
    <div style={{ minHeight: "100vh", display: "grid", placeItems: "center" }}>
      Signing you in...
    </div>
  );
};

export default AuthCallback;
