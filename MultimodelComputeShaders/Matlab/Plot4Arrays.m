function Plot4Arrays(A0, A1, A2, A3)
    tiledlayout(2, 2)
    nexttile
    plot(A0)
    title('Real plant')
    nexttile
    plot(A1)
    title('Cached model 1')
    nexttile
    plot(A2)
    title('Cached model 2')
    nexttile
    plot(A3)
    title('Cached model 3')
      

end

