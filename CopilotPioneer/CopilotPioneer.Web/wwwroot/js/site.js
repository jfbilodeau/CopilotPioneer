// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
((window) => {
    const pioneer = window.pioneer || {
        copyToClipboard: (text) => {
            const el = document.createElement('textarea')
            el.value = text
            document.body.appendChild(el)
            el.select()
            document.execCommand('copy')
            document.body.removeChild(el)
        },
        
        copyElementToClipboard: (selector) => {
            const el = document.querySelector(selector)
            const text = el.innerText
            
            pioneer.copyToClipboard(text)
        },
        
        
    }
    
    window.pioneer = pioneer
})(window)