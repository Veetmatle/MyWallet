// src/hooks/useAssetHints.ts
import { useState } from "react";

export interface AssetHintDto {
    symbol: string;
    name: string;
}

export function useAssetHints() {
    const [hints, setHints] = useState<AssetHintDto[]>([]);

    const fetchHints = async (category: string, query: string) => {
        try {
            const res = await fetch(
                `/api/asset/hints?category=${encodeURIComponent(category)}&query=${encodeURIComponent(query)}`
            );
            if (!res.ok) throw new Error();
            setHints(await res.json());
        } catch {
            setHints([]);
        }
    };

    const clearHints = () => setHints([]);

    return { hints, fetchHints, clearHints };
}
