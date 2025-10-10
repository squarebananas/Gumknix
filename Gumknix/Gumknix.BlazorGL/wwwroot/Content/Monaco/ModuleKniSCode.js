globalThis.moduleKniSCode =
{
    LoadScript: function (url)
    {
        return new Promise((resolve, reject) =>
        {
            if (document.querySelector(`script[src="${url}"]`))
            {
                resolve();
                return;
            }
            const script = document.createElement('script');
            script.src = url;
            script.onload = () => resolve();
            script.onerror = () => reject(new Error(`Failed to load ${url}`));
            document.head.appendChild(script);
        });
    }
};
