/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      colors: {
        'toeic-canvas': '#f4f9fc',
        'toeic-canvas-strong': '#eaf5fb',
        'toeic-surface': '#ffffff',
        'toeic-surface-soft': '#f8fbfd',
        'toeic-border': '#d9e7ef',
        'toeic-border-strong': '#b9d4e3',
        'toeic-primary': '#0787c8',
        'toeic-primary-hover': '#006fa8',
        'toeic-text': '#10222f',
        'toeic-text-muted': '#526879',
      },
      fontFamily: {
        sans: ['Geist', 'Inter', 'sans-serif'],
        serif: ['Source Serif 4', 'Georgia', 'serif'],
      }
    },
  },
  plugins: [],
}
