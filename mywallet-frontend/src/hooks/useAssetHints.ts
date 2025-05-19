import { useState } from "react";

export interface AssetHintDto {
    symbol: string;
    name: string;
}

export function useAssetHints() {
    const [hints, setHints] = useState<AssetHintDto[]>([]);

    const fetchHints = async (query: string, category: string) => {
        if (query.length < 2) {
            setHints([]);     
            return;
        }
        try {
            const res = await fetch(
                `http://localhost:5210/api/asset/search?category=${encodeURIComponent(category)}&query=${encodeURIComponent(query)}`
            );
            if (!res.ok) {
                setHints([]);
                return;
            }
            const data: AssetHintDto[] = await res.json();
            setHints(data);
        } catch {
            setHints([]);
        }
    };

    const clearHints = () => setHints([]);

    return { hints, fetchHints, clearHints };
}
