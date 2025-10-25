/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        './Views/**/*.cshtml',
        './Pages/**/*.cshtml',
    ],
    theme: {
        extend: {
            colors: {
                'usa-blue': '#1e3a8a',
                'usa-red': '#dc2626',
            },
        },
    },
    plugins: [],
}