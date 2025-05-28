// src/pages/PortfolioReportForm.tsx
import { useState } from "react";
import { useParams, useNavigate } from "react-router-dom";

const DATE_OPTIONS = [
    { label: "Ostatni tydzień", value: "week" },
    { label: "Ostatni miesiąc", value: "month" },
    { label: "Ostatni rok", value: "year" },
];

function getDateRange(option: string) {
    const end = new Date();
    const start = new Date();

    switch (option) {
        case "week":
            start.setDate(end.getDate() - 7);
            break;
        case "month":
            start.setMonth(end.getMonth() - 1);
            break;
        case "year":
            start.setFullYear(end.getFullYear() - 1);
            break;
        default:
            start.setDate(end.getDate() - 7);
    }

    // Ustaw start na 00:00:00 UTC
    start.setUTCHours(0, 0, 0, 0);
    // Ustaw end na 23:59:59 UTC
    end.setUTCHours(23, 59, 59, 999);

    return {
        start: start.toISOString(),
        end: end.toISOString(),
    };
}

export default function PortfolioReportForm() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const [dateOption, setDateOption] = useState("week");

    const handleOk = () => {
        if (!id) return;

        const { start, end } = getDateRange(dateOption);
        // Przekieruj na widok raportu z pełnymi datami w query string
        navigate(`/portfolio/${id}/report/view?start=${encodeURIComponent(start)}&end=${encodeURIComponent(end)}`);
    };

    return (
        <div className="dashboard-content">
            <h2>Raport transakcji portfela</h2>
            <label>Wybierz zakres:</label>
            <select
                value={dateOption}
                onChange={(e) => setDateOption(e.target.value)}
                style={{ marginLeft: 10 }}
            >
                {DATE_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>
                        {opt.label}
                    </option>
                ))}
            </select>
            <div style={{ marginTop: 16 }}>
                <button onClick={handleOk}>OK</button>
                <button onClick={() => navigate(-1)} style={{ marginLeft: 8 }}>
                    Anuluj / Wróć
                </button>
            </div>
        </div>
    );
}
