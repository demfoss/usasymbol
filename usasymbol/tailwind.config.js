const dynamicSymbolColors = ['blue', 'green', 'pink', 'red', 'amber', 'orange', 'cyan', 'purple', 'indigo', 'slate'];

/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        './Views/**/*.cshtml',
        './Pages/**/*.cshtml',
        './Models/**/*.cs',
        './Extensions/**/*.cs',
        './Helpers/**/*.cs',
        './Controllers/**/*.cs',
        './Services/**/*.cs',
        './wwwroot/**/*.js',
        './Content/**/*.cs',
    ],
    safelist: [
        'bg-gradient-to-r',
        'bg-gradient-to-br',
        'bg-gradient-to-b',
        ...dynamicSymbolColors.map((color) => `hover:bg-${color}-50`),
        ...dynamicSymbolColors.map((color) => `active:bg-${color}-50`),
        ...dynamicSymbolColors.map((color) => `group-hover:text-${color}-700`),
        ...dynamicSymbolColors.map((color) => `hover:border-${color}-500`),
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
