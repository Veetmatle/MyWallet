import { useState } from "react";
import { login } from "../api/user";

export default function Login() {
    const [usernameOrEmail, setUsernameOrEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");

    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            const user = await login(usernameOrEmail, password);
            console.log("Zalogowano jako:", user);
            localStorage.setItem("user", JSON.stringify(user));
            window.location.href = "/dashboard";
        } catch (err: any) {
            setError(err.message);
        }
    };

    return (
        <div style={{ padding: 20 }}>
            <h2>Logowanie</h2>
            <form onSubmit={handleLogin}>
                <input
                    type="text"
                    placeholder="Email lub login"
                    value={usernameOrEmail}
                    onChange={(e) => setUsernameOrEmail(e.target.value)}
                />
                <br />
                <input
                    type="password"
                    placeholder="Hasło"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                />
                <br />
                <button type="submit">Zaloguj się</button>
                {error && <p style={{ color: "red" }}>{error}</p>}
            </form>
        </div>
    );
}
