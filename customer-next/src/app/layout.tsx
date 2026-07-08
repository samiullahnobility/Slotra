import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'Slotra Customer',
  description: 'Book and manage Slotra appointments'
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
