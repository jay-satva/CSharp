import React, { useState } from 'react';

const API_BASE = 'http://localhost:5130';

const Signup = () => {
  const [form, setForm] = useState({ name: '', email: '', password: '', confirm: '' });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleIntuitSignup = () => {
    window.location.href = `${API_BASE}/auth/sso/connect`;
  };

  const handleChange = (e) => {
    setForm((prev) => ({ ...prev, [e.target.name]: e.target.value }));
  };

  const handleManualSignup = async (e) => {
    e.preventDefault();
    setError('');

    const { name, email, password, confirm } = form;

    if (!name || !email || !password || !confirm) {
      setError('All fields are required.');
      return;
    }
    if (password.length < 6) {
      setError('Password must be at least 6 characters.');
      return;
    }
    if (password !== confirm) {
      setError('Passwords do not match.');
      return;
    }

    setLoading(true);
    try {
      const res = await fetch(`${API_BASE}/auth/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ name, email, password }),
      });

      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        setError(data.message || 'Registration failed. Please try again.');
        return;
      }

      window.location.href = '/login';
    } catch {
      setError('Could not connect to the server. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;600&family=Instrument+Serif:ital@0;1&display=swap');

        .auth-root {
          min-height: 100vh;
          display: flex;
          align-items: center;
          justify-content: center;
          background: #f5f4f0;
          font-family: 'DM Sans', sans-serif;
          padding: 24px;
        }

        .auth-card {
          background: #ffffff;
          border-radius: 20px;
          box-shadow: 0 2px 4px rgba(0,0,0,0.04), 0 12px 40px rgba(0,0,0,0.08);
          width: 100%;
          max-width: 420px;
          padding: 48px 44px;
        }

        .auth-logo {
          display: flex;
          align-items: center;
          gap: 8px;
          margin-bottom: 32px;
        }

        .auth-logo-dot {
          width: 10px;
          height: 10px;
          border-radius: 50%;
          background: #2c6bed;
        }

        .auth-logo-text {
          font-family: 'Instrument Serif', serif;
          font-size: 18px;
          color: #1a1a1a;
          letter-spacing: -0.3px;
        }

        .auth-title {
          font-family: 'Instrument Serif', serif;
          font-size: 30px;
          font-weight: 400;
          color: #1a1a1a;
          margin: 0 0 6px;
          letter-spacing: -0.5px;
          line-height: 1.2;
        }

        .auth-subtitle {
          font-size: 14px;
          color: #888;
          margin: 0 0 32px;
        }

        .auth-label {
          display: block;
          font-size: 12px;
          font-weight: 600;
          color: #555;
          letter-spacing: 0.4px;
          text-transform: uppercase;
          margin-bottom: 6px;
        }

        .auth-input {
          width: 100%;
          padding: 11px 14px;
          font-size: 15px;
          font-family: 'DM Sans', sans-serif;
          color: #1a1a1a;
          background: #fafafa;
          border: 1.5px solid #e8e8e8;
          border-radius: 10px;
          outline: none;
          transition: border-color 0.15s, background 0.15s;
          box-sizing: border-box;
        }

        .auth-input:focus {
          border-color: #2c6bed;
          background: #fff;
        }

        .auth-input::placeholder {
          color: #bbb;
        }

        .auth-field {
          margin-bottom: 16px;
        }

        .auth-error {
          background: #fff2f2;
          border: 1px solid #ffd0d0;
          color: #c0392b;
          font-size: 13px;
          padding: 10px 14px;
          border-radius: 8px;
          margin-bottom: 16px;
        }

        .btn-primary-auth {
          width: 100%;
          padding: 13px;
          font-size: 15px;
          font-weight: 600;
          font-family: 'DM Sans', sans-serif;
          color: #fff;
          background: #1a1a1a;
          border: none;
          border-radius: 10px;
          cursor: pointer;
          transition: background 0.15s, transform 0.1s;
          margin-top: 4px;
        }

        .btn-primary-auth:hover:not(:disabled) {
          background: #333;
        }

        .btn-primary-auth:active:not(:disabled) {
          transform: scale(0.99);
        }

        .btn-primary-auth:disabled {
          opacity: 0.6;
          cursor: not-allowed;
        }

        .auth-divider {
          display: flex;
          align-items: center;
          gap: 12px;
          margin: 24px 0;
        }

        .auth-divider hr {
          flex: 1;
          border: none;
          border-top: 1px solid #ebebeb;
          margin: 0;
        }

        .auth-divider span {
          font-size: 12px;
          font-weight: 600;
          color: #bbb;
          letter-spacing: 0.5px;
          text-transform: uppercase;
        }

        .btn-intuit {
          width: 100%;
          display: flex;
          align-items: center;
          justify-content: center;
          gap: 10px;
          padding: 13px;
          font-size: 15px;
          font-weight: 600;
          font-family: 'DM Sans', sans-serif;
          color: #fff;
          background: #2c6bed;
          border: none;
          border-radius: 10px;
          cursor: pointer;
          transition: background 0.15s, transform 0.1s;
        }

        .btn-intuit:hover {
          background: #1a5bd8;
        }

        .btn-intuit:active {
          transform: scale(0.99);
        }

        .intuit-icon {
          width: 18px;
          height: 18px;
          background: #fff;
          border-radius: 3px;
          display: flex;
          align-items: center;
          justify-content: center;
          flex-shrink: 0;
        }

        .intuit-icon span {
          font-size: 11px;
          font-weight: 800;
          color: #2c6bed;
          line-height: 1;
        }

        .auth-footer {
          text-align: center;
          margin-top: 24px;
          font-size: 13px;
          color: #999;
        }

        .auth-footer a {
          color: #2c6bed;
          text-decoration: none;
          font-weight: 600;
        }

        .auth-footer a:hover {
          text-decoration: underline;
        }

        .password-hint {
          font-size: 11px;
          color: #bbb;
          margin-top: 5px;
        }
      `}</style>

      <div className="auth-root">
        <div className="auth-card">
          <div className="auth-logo">
            <div className="auth-logo-dot" />
            <span className="auth-logo-text">QuickBooks Connect</span>
          </div>

          <h1 className="auth-title">Create account</h1>
          <p className="auth-subtitle">Get started — it only takes a minute.</p>

          {error && <div className="auth-error">{error}</div>}

          <form onSubmit={handleManualSignup} noValidate>
            <div className="auth-field">
              <label className="auth-label">Full Name</label>
              <input
                type="text"
                name="name"
                className="auth-input"
                placeholder="Jane Smith"
                value={form.name}
                onChange={handleChange}
                autoComplete="name"
              />
            </div>
            <div className="auth-field">
              <label className="auth-label">Email</label>
              <input
                type="email"
                name="email"
                className="auth-input"
                placeholder="name@example.com"
                value={form.email}
                onChange={handleChange}
                autoComplete="email"
              />
            </div>
            <div className="auth-field">
              <label className="auth-label">Password</label>
              <input
                type="password"
                name="password"
                className="auth-input"
                placeholder="••••••••"
                value={form.password}
                onChange={handleChange}
                autoComplete="new-password"
              />
              <p className="password-hint">Minimum 6 characters</p>
            </div>
            <div className="auth-field">
              <label className="auth-label">Confirm Password</label>
              <input
                type="password"
                name="confirm"
                className="auth-input"
                placeholder="••••••••"
                value={form.confirm}
                onChange={handleChange}
                autoComplete="new-password"
              />
            </div>
            <button type="submit" className="btn-primary-auth" disabled={loading}>
              {loading ? 'Creating account…' : 'Create Account'}
            </button>
          </form>

          <div className="auth-divider">
            <hr /><span>or</span><hr />
          </div>

          <button className="btn-intuit" onClick={handleIntuitSignup}>
            <div className="intuit-icon"><span>Q</span></div>
            Continue with Intuit
          </button>

          <div className="auth-footer">
            Already have an account? <a href="/login">Sign In</a>
          </div>
        </div>
      </div>
    </>
  );
};

export default Signup;
