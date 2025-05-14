import { useEffect, useState } from "react";

interface PortfolioDto {
    id: number;
    name: string;
    description?: string;
    createdAt: string;
}

export default function Dashboard() {
    const [portfolios, setPortfolios] = useState<PortfolioDto[]>([]);
    const [error, setError] = useState("");

    const user = JSON.parse(localStorage.getItem("user") || "null");

    useEffect(() => {
        if (!user) {
            window.location.href = "/";
            return;
        }

        fetch(`/api/portfolio/user/${user.id}`)
            .then((res) => {
                if (!res.ok) throw new Error("Nie udało się pobrać portfeli");
                return res.json();
            })
            .then(setPortfolios)
            .catch((err) => setError(err.message));
    }, [user]);

    return (
        <div style={{ padding: "20px" }}>
            <h2>Twoje Portfele</h2>
            {error && <p style={{ color: "red" }}>{error}</p>}
            <ul>
                {portfolios.map((p) => (
                    <li key={p.id}>
                        <strong>{p.name}</strong> — {p.description || "brak opisu"}
                    </li>
                ))}
            </ul>
        </div>
    );
}
