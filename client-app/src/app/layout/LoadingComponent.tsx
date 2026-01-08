import React from "react";

interface Props {
    inverted?: boolean;
    content?: string;
}

export default function LoadingComponent({ inverted = true, content = 'Ladataan...' }: Props) {
    return (
        <div className="loading-container" style={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            minHeight: '200px',
            gap: 'var(--spacing-lg)'
        }}>
            <div className="loading-spinner" style={{
                width: '48px',
                height: '48px',
                border: '4px solid var(--border-color)',
                borderTopColor: 'var(--accent-primary)',
                borderRadius: '50%',
                animation: 'spin 1s linear infinite'
            }} />
            <p style={{
                color: 'var(--text-secondary)',
                fontSize: 'var(--font-size-base)',
                margin: 0
            }}>
                {content}
            </p>
            <style>{`
                @keyframes spin {
                    to { transform: rotate(360deg); }
                }
            `}</style>
        </div>
    )
}
