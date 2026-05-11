window.REDI_DEFAULT_QUESTIONS = {
  topics: [
    { id: 1, name: 'HTML',              description: 'HyperText Markup Language' },
    { id: 2, name: 'CSS',               description: 'Cascading Style Sheets' },
    { id: 3, name: 'JavaScript',        description: 'Programming for the web' },
    { id: 4, name: 'Responsive Design', description: 'Mobile-first development' },
    { id: 5, name: 'Version Control',   description: 'Code management with Git' }
  ],
  questions: [
    {
      id: 1,
      text: 'What does HTML stand for?',
      options: [
        'Hyper Text Markup Language',
        'High Tech Modern Language',
        'Hyper Transfer Markup Language',
        'Home Tool Markup Language'
      ],
      correctIndex: 0
    },
    {
      id: 2,
      text: 'Which CSS property changes the text color?',
      options: ['text-color', 'font-color', 'color', 'text-style'],
      correctIndex: 2
    },
    {
      id: 3,
      text: 'Which HTML tag is used to create a hyperlink?',
      options: ['<link>', '<a>', '<href>', '<url>'],
      correctIndex: 1
    },
    {
      id: 4,
      text: 'Which CSS property is used to make text bold?',
      options: ['text-size: bold', 'font-weight: bold', 'text-weight: bold', 'bold: true'],
      correctIndex: 1
    },
    {
      id: 5,
      text: 'Which JavaScript method selects an element by its ID?',
      options: [
        'document.getElement()',
        'document.querySelector()',
        'document.getElementById()',
        'document.findById()'
      ],
      correctIndex: 2
    },
    {
      id: 6,
      text: 'Which CSS property controls the space outside an element\'s border?',
      options: ['padding', 'margin', 'border-spacing', 'spacing'],
      correctIndex: 1
    },
    {
      id: 7,
      text: 'In HTML, which attribute provides alternative text for an image?',
      options: ['title', 'src', 'alt', 'href'],
      correctIndex: 2
    },
    {
      id: 8,
      text: 'Which JavaScript keyword declares a variable that cannot be reassigned?',
      options: ['var', 'let', 'const', 'static'],
      correctIndex: 2
    }
  ],
  openQuestions: [
    { id: 101, text: 'Describe the difference between margin and padding in CSS.' },
    { id: 102, text: 'What is the purpose of the <head> element in HTML?' },
    { id: 103, text: 'Explain what a function is in JavaScript and give a simple example.' }
  ]
};
