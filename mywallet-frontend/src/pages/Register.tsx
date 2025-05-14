import { useState } from "react";
import { register } from "../api/user";

export default function Register() {
    const [username, setUsername] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [success, setSuccess] = useState("");
    const [error, setError] = useState("");

    const handleRegister = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            const result = await register(username, email, password);
            setSuccess(result);
            setError("");
        } catch (err: any) {
            setError(err.message);
            setSuccess("");
        }
    };

    return (
        <div style={{ padding: 20 }}>
            <h2>Rejestracja</h2>
            <form onSubmit={handleRegister}>
                <input
                    type="text"
                    placeholder="Login"
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                />
                <br />
                <input
                    type="email"
                    placeholder="E-mail"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                />
                <br />
                <input
                    type="password"
                    placeholder="Hasło"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                />
                <br />
                <button type="submit">Zarejestruj się</button>
                {success && <p style={{ color: "green" }}>{success}</p>}
                {error && <p style={{ color: "red" }}>{error}</p>}
            </form>
        </div>
    );
}
