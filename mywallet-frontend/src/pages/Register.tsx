import { useState } from "react";
import { Link } from "react-router-dom";
import { register } from "../api/user";
import "../auth.css";

export default function Register() {
    const [username, setUsername] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");

    const [errors, setErrors] = useState<{ [key: string]: string }>({});
    const [success, setSuccess] = useState("");

    const validate = () => {
        const newErrors: { [key: string]: string } = {};

        if (!username.trim()) newErrors.username = "Login jest wymagany.";
        else if (username.length > 50) newErrors.username = "Login może mieć maksymalnie 50 znaków.";

        if (!email.trim()) newErrors.email = "E-mail jest wymagany.";
        else if (!/^\S+@\S+\.\S+$/.test(email)) newErrors.email = "Nieprawidłowy adres e-mail.";

        if (!password) newErrors.password = "Hasło jest wymagane.";
        else if (password.length < 6) newErrors.password = "Hasło musi mieć co najmniej 6 znaków.";

        if (!confirmPassword) newErrors.confirmPassword = "Potwierdzenie hasła jest wymagane.";
        else if (password !== confirmPassword) newErrors.confirmPassword = "Hasła nie są takie same.";

        setErrors(newErrors);
        return Object.keys(newErrors).length === 0;
    };

    const handleRegister = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!validate()) return;

        try {
            const response = await fetch("/api/user/register", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ username, email, password }),
            });

            if (response.status === 400) {
                const data = await response.json();
                const backendErrors: { [key: string]: string } = {};
                for (const key in data.errors) {
                    backendErrors[key.toLowerCase()] = data.errors[key][0]; // zakładamy jeden błąd na pole
                }
                setErrors(backendErrors);
                setSuccess("");
                return;
            }

            if (!response.ok) throw new Error("Rejestracja nie powiodła się");

            const message = await response.text();
            setSuccess(message);
            setErrors({});
        } catch (err: any) {
            setErrors({ global: err.message });
            setSuccess("");
        }
    };

    return (
        <div className="auth-container">
            <div className="auth-card">
                <img src="/logo192.png" alt="MyWallet Logo" className="auth-logo" />
                <h2 className="auth-title">Rejestracja w MyWallet</h2>
                <form onSubmit={handleRegister} className="auth-form">
                    <div className="form-group">
                        <label htmlFor="username">Login</label>
                        <input
                            id="username"
                            type="text"
                            placeholder="Wybierz login"
                            value={username}
                            onChange={(e) => setUsername(e.target.value)}
                            className="form-control"
                        />
                        {errors.username && <div className="error-message">{errors.username}</div>}
                    </div>
                    <div className="form-group">
                        <label htmlFor="email">E-mail</label>
                        <input
                            id="email"
                            type="email"
                            placeholder="Wprowadź adres e-mail"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            className="form-control"
                        />
                        {errors.email && <div className="error-message">{errors.email}</div>}
                    </div>
                    <div className="form-group">
                        <label htmlFor="password">Hasło</label>
                        <input
                            id="password"
                            type="password"
                            placeholder="Wybierz bezpieczne hasło"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            className="form-control"
                        />
                        {errors.password && <div className="error-message">{errors.password}</div>}
                    </div>
                    <div className="form-group">
                        <label htmlFor="confirmPassword">Powtórz hasło</label>
                        <input
                            id="confirmPassword"
                            type="password"
                            placeholder="Powtórz hasło"
                            value={confirmPassword}
                            onChange={(e) => setConfirmPassword(e.target.value)}
                            className="form-control"
                        />
                        {errors.confirmPassword && <div className="error-message">{errors.confirmPassword}</div>}
                    </div>
                    <button type="submit" className="btn btn-primary">
                        Zarejestruj się
                    </button>
                    {errors.global && <div className="error-message">{errors.global}</div>}
                    {success && <div className="success-message">{success}</div>}
                </form>
                <div className="auth-footer">
                    Masz już konto?
                    <Link to="/" className="auth-link">
                        Zaloguj się!
                    </Link>
                </div>
            </div>
        </div>
    );
}